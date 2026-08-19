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

        // MercadoPago sends data.id in query param or body
        var dataId = ctx.Request.Query["data.id"].FirstOrDefault() ?? string.Empty;
        var type = ctx.Request.Query["type"].FirstOrDefault() ?? string.Empty;

        var command = new ProcessWebhookCommand(xSignature, xRequestId, dataId, type);
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }
}
