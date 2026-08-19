using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.UpdateCartItem;

public sealed class UpdateCartItemCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCartItemCommand, Result>
{
    public async Task<Result> Handle(UpdateCartItemCommand request, CancellationToken ct)
    {
        var cartItem = await dbContext.Set<CartItem>()
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == request.ItemId && ci.Cart.UserId == request.UserId, ct);

        if (cartItem is null)
            return Result.NotFound("Item no encontrado en el carrito.");

        var variant = await dbContext.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == cartItem.VariantId, ct);

        if (variant is null || !variant.IsActive)
            return Result.NotFound("Variante no disponible.");

        if (request.Quantity > variant.Stock)
            return Result.Failure(new Error("INSUFFICIENT_STOCK",
                $"Stock insuficiente. Disponible: {variant.Stock}."));

        cartItem.Quantity = request.Quantity;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
