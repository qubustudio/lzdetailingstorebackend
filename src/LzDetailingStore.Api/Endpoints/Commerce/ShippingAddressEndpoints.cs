using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Commerce.Commands.CreateAddress;
using LzDetailingStore.Application.Features.Commerce.Commands.DeleteAddress;
using LzDetailingStore.Application.Features.Commerce.Commands.UpdateAddress;
using LzDetailingStore.Application.Features.Commerce.Queries.GetAddresses;
using MediatR;
using System.Security.Claims;

namespace LzDetailingStore.Api.Endpoints.Commerce;

public sealed class ShippingAddressEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/direcciones")
            .WithTags("Shipping Addresses")
            .RequireAuthorization();

        group.MapGet("/", GetAddresses);
        group.MapPost("/", CreateAddress);
        group.MapPut("/{id:guid}", UpdateAddress);
        group.MapDelete("/{id:guid}", DeleteAddress);
    }

    private static Guid GetUserId(HttpContext ctx) =>
        Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? ctx.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

    private static async Task<IResult> GetAddresses(HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAddressesQuery(GetUserId(ctx)), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> CreateAddress(
        CreateAddressCommand command, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var cmd = command with { UserId = GetUserId(ctx) };
        var result = await sender.Send(cmd, ct);
        return result.ToCreatedResponse("/api/v1/direcciones");
    }

    private static async Task<IResult> UpdateAddress(
        Guid id, UpdateAddressCommand command, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID de la ruta no coincide." });

        var cmd = command with { UserId = GetUserId(ctx) };
        var result = await sender.Send(cmd, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteAddress(
        Guid id, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteAddressCommand(id, GetUserId(ctx)), ct);
        return result.ToApiResponse();
    }
}
