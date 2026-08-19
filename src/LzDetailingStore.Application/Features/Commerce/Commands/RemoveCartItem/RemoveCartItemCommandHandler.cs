using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCartItemCommand, Result>
{
    public async Task<Result> Handle(RemoveCartItemCommand request, CancellationToken ct)
    {
        var cartItem = await dbContext.Set<CartItem>()
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == request.ItemId && ci.Cart.UserId == request.UserId, ct);

        if (cartItem is null)
            return Result.NotFound("Item no encontrado en el carrito.");

        dbContext.Set<CartItem>().Remove(cartItem);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
