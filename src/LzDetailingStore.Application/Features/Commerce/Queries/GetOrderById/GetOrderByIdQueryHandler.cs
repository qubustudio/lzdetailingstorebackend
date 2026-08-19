using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(DbContext dbContext)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, ct);

        if (order is null)
            return Result<OrderDto>.NotFound("Pedido no encontrado.");

        var dto = new OrderDto(
            order.Id,
            order.Status.ToString(),
            order.Subtotal,
            order.ShippingCost,
            order.Total,
            order.TrackingNumber,
            order.TrackingUrl,
            order.CreatedAt,
            order.Items.Select(i => new OrderItemDto(
                i.Id, i.ProductName, i.VariantName, i.Sku,
                i.ImageUrl, i.UnitPrice, i.Quantity, i.Subtotal
            )).ToList());

        return dto;
    }
}
