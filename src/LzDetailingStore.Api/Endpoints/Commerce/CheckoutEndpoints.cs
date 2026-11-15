using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;
using MediatR;
using System.Security.Claims;

namespace LzDetailingStore.Api.Endpoints.Commerce;

public sealed class CheckoutEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/checkout")
            .WithTags("Checkout")
            .RequireAuthorization();

        group.MapPost("/", CreateCheckout);
    }

    private static async Task<IResult> CreateCheckout(
        HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var userId = Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? ctx.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

        // RemoteIpAddress already reflects X-Forwarded-For from trusted proxies (UseForwardedHeaders).
        // Both values are optional: the handler normalizes them and stores null when missing or invalid.
        var clientIp = ctx.Connection.RemoteIpAddress?.ToString();
        var userAgent = ctx.Request.Headers.UserAgent.ToString();

        var result = await sender.Send(new CreateCheckoutCommand(userId, clientIp, userAgent), ct);
        return result.ToApiResponse();
    }
}
