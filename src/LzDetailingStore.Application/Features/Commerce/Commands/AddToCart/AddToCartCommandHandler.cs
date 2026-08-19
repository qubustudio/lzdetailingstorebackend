using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.AddToCart;

public sealed class AddToCartCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddToCartCommand, Result>
{
    public async Task<Result> Handle(AddToCartCommand request, CancellationToken ct)
    {
        // Validate variant exists, is active, and has stock
        var variant = await dbContext.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.IsActive, ct);

        if (variant is null)
            return Result.NotFound("Variante no encontrada o no disponible.");

        if (variant.Stock < request.Quantity)
            return Result.Failure(new Error("INSUFFICIENT_STOCK", 
                $"Stock insuficiente. Disponible: {variant.Stock}."));

        // Get or create cart
        var cart = await dbContext.Set<Cart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null)
        {
            cart = new Cart { UserId = request.UserId };
            await dbContext.Set<Cart>().AddAsync(cart, ct);
        }

        // Upsert: if variant already in cart, update quantity
        var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == request.VariantId);

        if (existingItem is not null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (newQuantity > variant.Stock)
                return Result.Failure(new Error("INSUFFICIENT_STOCK",
                    $"Stock insuficiente. Disponible: {variant.Stock}, en carrito: {existingItem.Quantity}."));

            existingItem.Quantity = newQuantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                VariantId = request.VariantId,
                Quantity = request.Quantity
            });
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
