using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.MergeCart;

public sealed class MergeCartCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MergeCartCommand, Result>
{
    public async Task<Result> Handle(MergeCartCommand request, CancellationToken ct)
    {
        // Get or create cart
        var cart = await dbContext.Set<Cart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null)
        {
            cart = new Cart { UserId = request.UserId };
            await dbContext.Set<Cart>().AddAsync(cart, ct);
        }

        foreach (var localItem in request.Items)
        {
            // Skip if variant already exists in server cart (server wins)
            if (cart.Items.Any(i => i.VariantId == localItem.VariantId))
                continue;

            // Validate variant exists and is active
            var variant = await dbContext.Set<ProductVariant>()
                .FirstOrDefaultAsync(v => v.Id == localItem.VariantId && v.IsActive, ct);

            if (variant is null)
                continue; // Skip invalid/inactive silently

            var quantity = Math.Min(localItem.Quantity, variant.Stock);
            if (quantity <= 0)
                continue;

            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                VariantId = localItem.VariantId,
                Quantity = quantity
            });
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
