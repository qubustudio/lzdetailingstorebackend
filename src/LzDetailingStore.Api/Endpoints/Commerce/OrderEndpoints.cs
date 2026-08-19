using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Commerce.Queries.GetOrderById;
using LzDetailingStore.Application.Features.Commerce.Queries.GetOrders;
using MediatR;
using System.Security.Claims;

namespace LzDetailingStore.Api.Endpoints.Commerce;

public sealed class OrderEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pedidos")
            .WithTags("Orders")
            .RequireAuthorization();

        group.MapGet("/", GetOrders);
        group.MapGet("/{id:guid}", GetOrderById);
    }

    private static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? ctx.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    private static async Task<IResult> GetOrders(
        HttpContext ctx, ISender sender, CancellationToken ct, int page = 1, int limit = 20)
    {
        var result = await sender.Send(new GetOrdersQuery(GetUserId(ctx), page, limit), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetOrderById(
        Guid id, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id, GetUserId(ctx)), ct);
        return result.ToApiResponse();
    }
}
