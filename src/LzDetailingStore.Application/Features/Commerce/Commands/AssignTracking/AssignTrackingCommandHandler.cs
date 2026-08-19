using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.AssignTracking;

public sealed class AssignTrackingCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AssignTrackingCommand, Result>
{
    public async Task<Result> Handle(AssignTrackingCommand request, CancellationToken ct)
    {
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        if (order is null)
            return Result.NotFound("Pedido no encontrado.");

        order.TrackingNumber = request.TrackingNumber;
        order.TrackingUrl = request.TrackingUrl;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
