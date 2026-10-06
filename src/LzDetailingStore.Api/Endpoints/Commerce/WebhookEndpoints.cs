using System.Text.Json;
using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Commerce.Commands.ProcessWebhook;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Commerce;

public sealed class WebhookEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/webhooks")
            .WithTags("Webhooks");

        // No RequireAuthorization — MercadoPago uses signature validation
        group.MapPost("/mercadopago", ProcessMercadoPago);
    }

    private static async Task<IResult> ProcessMercadoPago(
        HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var xSignature = ctx.Request.Headers["x-signature"].FirstOrDefault() ?? string.Empty;
        var xRequestId = ctx.Request.Headers["x-request-id"].FirstOrDefault() ?? string.Empty;

        // MercadoPago sends data.id and type in query params, but may also send
        // them in the JSON body. Read query first, then fall back to the body.
        var dataId = ctx.Request.Query["data.id"].FirstOrDefault() ?? string.Empty;
        var type = ctx.Request.Query["type"].FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(dataId) || string.IsNullOrEmpty(type))
        {
            try
            {
                ctx.Request.EnableBuffering();
                using var doc = await JsonDocument.ParseAsync(ctx.Request.Body, cancellationToken: ct);
                var root = doc.RootElement;

                if (string.IsNullOrEmpty(type) &&
                    root.TryGetProperty("type", out var typeEl))
                    type = typeEl.GetString() ?? string.Empty;

                if (string.IsNullOrEmpty(dataId) &&
                    root.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("id", out var idEl))
                {
                    dataId = idEl.ValueKind == JsonValueKind.Number
                        ? idEl.GetRawText()
                        : idEl.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Body was not valid JSON; proceed with whatever we have.
            }
        }

        var command = new ProcessWebhookCommand(xSignature, xRequestId, dataId, type);
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }
}
