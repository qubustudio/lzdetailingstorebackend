using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.Domain.Services;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAdminOrderById;

public sealed class GetAdminOrderByIdQueryHandler(DbContext dbContext)
    : IRequestHandler<GetAdminOrderByIdQuery, Result<AdminOrderDetailDto>>
{
    public async Task<Result<AdminOrderDetailDto>> Handle(GetAdminOrderByIdQuery request, CancellationToken ct)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order is null)
            return Result<AdminOrderDetailDto>.NotFound("Pedido no encontrado.");

        var customer = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => u.Id == order.UserId)
            .Select(u => new AdminOrderCustomerDto(u.Id, u.Nombre, u.Email, u.Telefono))
            .FirstOrDefaultAsync(ct);

        // The address is read even if soft-deleted: it is the one the order was shipped to.
        var address = await dbContext.Set<ShippingAddress>()
            .AsNoTracking()
            .Where(a => a.Id == order.ShippingAddressId)
            .Select(a => new ShippingAddressDto(
                a.Id, a.Nombre, a.Calle, a.NumeroExterior, a.NumeroInterior, a.Colonia,
                a.Ciudad, a.Estado, a.CodigoPostal, a.Telefono, a.Referencia, a.EsPrincipal))
            .FirstOrDefaultAsync(ct);

        var showMoney = request.IncluyeMontos;

        return new AdminOrderDetailDto(
            showMoney,
            order.Id,
            order.Status.ToString(),
            OrderStatusMachine.GetAllowedTransitions(order.Status).Select(s => s.ToString()).ToList(),
            customer,
            address,
            showMoney ? order.Subtotal : null,
            showMoney ? order.ShippingCost : null,
            showMoney ? order.Total : null,
            order.TrackingNumber,
            order.TrackingUrl,
            order.Notes,
            order.MercadoPagoPaymentId,
            order.CreatedAt,
            order.UpdatedAt,
            order.Items
                .OrderBy(i => i.ProductName)
                .Select(i => new AdminOrderItemDto(
                    i.Id, i.VariantId, i.ProductName, i.VariantName, i.Sku, i.ImageUrl,
                    showMoney ? i.UnitPrice : null,
                    i.Quantity,
                    showMoney ? i.Subtotal : null))
                .ToList());
    }
}
