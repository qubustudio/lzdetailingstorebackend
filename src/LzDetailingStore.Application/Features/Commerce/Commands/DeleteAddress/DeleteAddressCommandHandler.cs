using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.DeleteAddress;

public sealed class DeleteAddressCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAddressCommand, Result>
{
    public async Task<Result> Handle(DeleteAddressCommand request, CancellationToken ct)
    {
        var address = await dbContext.Set<ShippingAddress>()
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId && a.IsActive, ct);

        if (address is null)
            return Result.NotFound("Dirección no encontrada.");

        address.IsActive = false;
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
