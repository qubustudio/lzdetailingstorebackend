using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetCart;

public sealed class GetCartQueryHandler(DbContext dbContext)
    : IRequestHandler<GetCartQuery, Result<CartDto>>
{
    public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken ct)
    {
        var cart = await dbContext.Set<Cart>()
            .Include(c => c.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null)
        {
            return new CartDto(Guid.Empty, [], 0, 0);
        }

        var items = cart.Items.Select(item => new CartItemDto(
            item.Id,
            item.VariantId,
            item.Variant.Product.Name,
            item.Variant.Nombre,
            item.Variant.Sku,
            item.Variant.Product.ImageUrl,
            item.Variant.PrecioVenta,
            item.Quantity,
            item.Variant.PrecioVenta * item.Quantity,
            item.Variant.Stock
        )).ToList();

        var subtotal = items.Sum(i => i.Subtotal);
        var totalItems = items.Sum(i => i.Quantity);

        return new CartDto(cart.Id, items, subtotal, totalItems);
    }
}
