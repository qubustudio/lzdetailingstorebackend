using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Commerce.Commands.AssignTracking;
using LzDetailingStore.Application.Features.Commerce.Commands.UpdateOrderStatus;
using LzDetailingStore.Application.Features.Commerce.Queries.GetAllOrders;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Commerce;

public sealed class AdminOrderEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/pedidos")
            .WithTags("Admin - Orders")
            .RequireAuthorization("pedidos.ver");

        group.MapGet("/", GetAllOrders)
            .RequireAuthorization("pedidos.ver");

        group.MapPut("/{id:guid}/status", UpdateOrderStatus)
            .RequireAuthorization("pedidos.editar");

        group.MapPut("/{id:guid}/tracking", AssignTracking)
            .RequireAuthorization("pedidos.editar");
    }

    private static async Task<IResult> GetAllOrders(
        ISender sender, CancellationToken ct,
        int page = 1, int limit = 20, OrderStatus? status = null)
    {
        var result = await sender.Send(new GetAllOrdersQuery(page, limit, status), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> UpdateOrderStatus(
        Guid id, UpdateOrderStatusRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateOrderStatusCommand(id, request.Status), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> AssignTracking(
        Guid id, AssignTrackingCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.OrderId)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID de la ruta no coincide." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }
}

public sealed record UpdateOrderStatusRequest(OrderStatus Status);
