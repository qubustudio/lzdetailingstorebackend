using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Services;

public sealed class MercadoPagoService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MercadoPagoService> logger) : IMercadoPagoService
{
    private string AccessToken => configuration["MercadoPago:AccessToken"] ?? string.Empty;
    private string WebhookSecret => configuration["MercadoPago:WebhookSecret"] ?? string.Empty;

    // Explicit opt-in to skip signature validation. Only for local development
    // without a webhook secret. Never enable in staging/production.
    private bool SkipSignatureValidation =>
        configuration.GetValue("MercadoPago:SkipSignatureValidation", false);

    public async Task<Result<MercadoPagoPreferenceResult>> CreatePreferenceAsync(
        string externalReference,
        IList<MercadoPagoItem> items,
        MercadoPagoBackUrls backUrls,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("MercadoPago");

        var mappedItems = items.Select(i => new
        {
            title = i.Title,
            quantity = i.Quantity,
            unit_price = i.UnitPrice,
            currency_id = "MXN"
        });

        var backUrlsPayload = new
        {
            success = backUrls.Success,
            failure = backUrls.Failure,
            pending = backUrls.Pending
        };

        // MercadoPago rejects auto_return when back_urls point to localhost
        // (it cannot validate them as public URLs). Only enable auto_return
        // when the success URL is a public https URL (e.g. staging/production).
        var canAutoReturn = backUrls.Success.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !backUrls.Success.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        object payload = canAutoReturn
            ? new
            {
                items = mappedItems,
                back_urls = backUrlsPayload,
                auto_return = "approved",
                external_reference = externalReference
            }
            : new
            {
                items = mappedItems,
                back_urls = backUrlsPayload,
                external_reference = externalReference
            };

        try
        {
            var response = await client.PostAsJsonAsync("checkout/preferences", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("MercadoPago CreatePreference failed: {StatusCode} {Error}",
                    response.StatusCode, error);
                return Result<MercadoPagoPreferenceResult>.Failure(
                    new Error("MERCADOPAGO_ERROR", "Error al crear preferencia de pago."));
            }

            var result = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>(ct);

            if (result is null || string.IsNullOrEmpty(result.InitPoint) || string.IsNullOrEmpty(result.Id))
            {
                logger.LogError("MercadoPago CreatePreference returned incomplete data.");
                return Result<MercadoPagoPreferenceResult>.Failure(
                    new Error("MERCADOPAGO_ERROR", "Respuesta incompleta de MercadoPago."));
            }

            return Result<MercadoPagoPreferenceResult>.Success(
                new MercadoPagoPreferenceResult(result.Id, result.InitPoint));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MercadoPago CreatePreference exception");
            return Result<MercadoPagoPreferenceResult>.Failure(
                new Error("MERCADOPAGO_ERROR", "Error de comunicación con MercadoPago."));
        }
    }

    public bool ValidateWebhookSignature(string xSignature, string xRequestId, string dataId)
    {
        if (string.IsNullOrWhiteSpace(WebhookSecret))
        {
            // Fail closed by default. Only accept unsigned webhooks when the
            // developer explicitly opts in via MercadoPago:SkipSignatureValidation.
            if (SkipSignatureValidation)
            {
                logger.LogWarning(
                    "MercadoPago signature validation SKIPPED (SkipSignatureValidation=true). " +
                    "This must never be enabled outside local development.");
                return true;
            }

            logger.LogError("MercadoPago WebhookSecret not configured; rejecting webhook.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(xSignature))
            return false;

        try
        {
            // Parse x-signature header: ts=...,v1=...
            var parts = xSignature.Split(',')
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

            if (!parts.TryGetValue("ts", out var ts) || !parts.TryGetValue("v1", out var v1))
                return false;

            // Build manifest string per MercadoPago spec
            var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";

            // HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();

            // Constant-time comparison to avoid timing attacks
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(v1));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MercadoPago webhook signature validation failed");
            return false;
        }
    }

    public async Task<Result<MercadoPagoPaymentInfo>> GetPaymentAsync(
        string paymentId, CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("MercadoPago");

        try
        {
            var response = await client.GetAsync($"v1/payments/{paymentId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("MercadoPago GetPayment failed: {StatusCode}", response.StatusCode);
                return Result<MercadoPagoPaymentInfo>.Failure(
                    new Error("MERCADOPAGO_ERROR", "Error al consultar pago."));
            }

            var result = await response.Content.ReadFromJsonAsync<MercadoPagoPaymentResponse>(ct);

            if (result is null)
                return Result<MercadoPagoPaymentInfo>.Failure(
                    new Error("MERCADOPAGO_ERROR", "Respuesta vacía de MercadoPago."));

            return new MercadoPagoPaymentInfo(
                result.Id.ToString(),
                result.Status ?? string.Empty,
                result.TransactionAmount,
                result.ExternalReference);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MercadoPago GetPayment exception");
            return Result<MercadoPagoPaymentInfo>.Failure(
                new Error("MERCADOPAGO_ERROR", "Error de comunicación con MercadoPago."));
        }
    }

    private sealed record MercadoPagoPreferenceResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("init_point")] string? InitPoint);

    private sealed record MercadoPagoPaymentResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount,
        [property: JsonPropertyName("external_reference")] string? ExternalReference);
}
