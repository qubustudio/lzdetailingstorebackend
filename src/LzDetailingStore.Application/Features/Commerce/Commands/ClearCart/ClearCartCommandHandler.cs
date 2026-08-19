using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.ClearCart;

public sealed class ClearCartCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ClearCartCommand, Result>
{
    public async Task<Result> Handle(ClearCartCommand request, CancellationToken ct)
    {
        var cart = await dbContext.Set<Cart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, ct);

        if (cart is null)
            return Result.Success(); // No cart = already empty

        dbContext.Set<CartItem>().RemoveRange(cart.Items);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
