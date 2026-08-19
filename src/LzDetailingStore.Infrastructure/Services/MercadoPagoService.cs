using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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

    public async Task<Result<string>> CreatePreferenceAsync(
        string externalReference,
        IList<MercadoPagoItem> items,
        MercadoPagoBackUrls backUrls,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("MercadoPago");

        var payload = new
        {
            items = items.Select(i => new
            {
                title = i.Title,
                quantity = i.Quantity,
                unit_price = i.UnitPrice,
                currency_id = "MXN"
            }),
            back_urls = new
            {
                success = backUrls.Success,
                failure = backUrls.Failure,
                pending = backUrls.Pending
            },
            auto_return = "approved",
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
                return Result<string>.Failure(
                    new Error("MERCADOPAGO_ERROR", "Error al crear preferencia de pago."));
            }

            var result = await response.Content.ReadFromJsonAsync<MercadoPagoPreferenceResponse>(ct);
            return Result<string>.Success(result?.InitPoint ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MercadoPago CreatePreference exception");
            return Result<string>.Failure(
                new Error("MERCADOPAGO_ERROR", "Error de comunicación con MercadoPago."));
        }
    }

    public bool ValidateWebhookSignature(string xSignature, string xRequestId, string dataId)
    {
        if (string.IsNullOrWhiteSpace(WebhookSecret))
        {
            logger.LogWarning("MercadoPago WebhookSecret not configured, skipping validation");
            return true; // In dev, accept all
        }

        try
        {
            // Parse x-signature header: ts=...,v1=...
            var parts = xSignature.Split(',')
                .Select(p => p.Split('=', 2))
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim());

            if (!parts.TryGetValue("ts", out var ts) || !parts.TryGetValue("v1", out var v1))
                return false;

            // Build manifest string
            var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";

            // HMAC-SHA256
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();

            return computed == v1;
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

    private sealed record MercadoPagoPreferenceResponse(string? InitPoint);

    private sealed record MercadoPagoPaymentResponse(
        long Id,
        string? Status,
        decimal TransactionAmount,
        string? ExternalReference);
}
