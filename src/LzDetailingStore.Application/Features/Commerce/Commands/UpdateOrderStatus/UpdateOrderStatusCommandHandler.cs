using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.Domain.Services;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateOrderStatusCommand request, CancellationToken ct)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order is null)
            return Result.NotFound("Pedido no encontrado.");

        if (!OrderStatusMachine.CanTransition(order.Status, request.NewStatus))
            return Result.Failure(new Error("INVALID_TRANSITION",
                $"No se puede cambiar de '{order.Status}' a '{request.NewStatus}'."));

        order.Status = request.NewStatus;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
