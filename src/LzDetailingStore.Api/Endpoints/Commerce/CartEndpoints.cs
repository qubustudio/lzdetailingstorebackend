using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Commerce.Commands.AddToCart;
using LzDetailingStore.Application.Features.Commerce.Commands.ClearCart;
using LzDetailingStore.Application.Features.Commerce.Commands.MergeCart;
using LzDetailingStore.Application.Features.Commerce.Commands.RemoveCartItem;
using LzDetailingStore.Application.Features.Commerce.Commands.UpdateCartItem;
using LzDetailingStore.Application.Features.Commerce.Queries.GetCart;
using MediatR;
using System.Security.Claims;

namespace LzDetailingStore.Api.Endpoints.Commerce;

public sealed class CartEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/carrito")
            .WithTags("Cart")
            .RequireAuthorization();

        group.MapGet("/", GetCart);
        group.MapPost("/items", AddToCart);
        group.MapPut("/items/{itemId:guid}", UpdateCartItem);
        group.MapDelete("/items/{itemId:guid}", RemoveCartItem);
        group.MapDelete("/", ClearCart);
        group.MapPost("/merge", MergeCart);
    }

    private static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? ctx.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    private static async Task<IResult> GetCart(HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetCartQuery(GetUserId(ctx)), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> AddToCart(
        AddToCartRequest request, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var command = new AddToCartCommand(GetUserId(ctx), request.VariantId, request.Quantity);
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> UpdateCartItem(
        Guid itemId, UpdateCartItemRequest request, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var command = new UpdateCartItemCommand(GetUserId(ctx), itemId, request.Quantity);
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> RemoveCartItem(
        Guid itemId, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveCartItemCommand(GetUserId(ctx), itemId), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> ClearCart(HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ClearCartCommand(GetUserId(ctx)), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> MergeCart(
        MergeCartCommand command, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var merged = command with { UserId = GetUserId(ctx) };
        var result = await sender.Send(merged, ct);
        return result.ToApiResponse();
    }
}

public sealed record AddToCartRequest(Guid VariantId, int Quantity);
public sealed record UpdateCartItemRequest(int Quantity);
