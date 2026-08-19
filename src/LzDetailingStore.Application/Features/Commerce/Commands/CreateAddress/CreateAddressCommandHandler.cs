using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateAddress;

public sealed class CreateAddressCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateAddressCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAddressCommand request, CancellationToken ct)
    {
        // If this is marked as principal, unset any existing principal
        if (request.EsPrincipal)
        {
            var currentPrincipal = await dbContext.Set<ShippingAddress>()
                .Where(a => a.UserId == request.UserId && a.EsPrincipal && a.IsActive)
                .ToListAsync(ct);

            foreach (var addr in currentPrincipal)
                addr.EsPrincipal = false;
        }

        var address = new ShippingAddress
        {
            UserId = request.UserId,
            Nombre = request.Nombre,
            Calle = request.Calle,
            NumeroExterior = request.NumeroExterior,
            NumeroInterior = request.NumeroInterior,
            Colonia = request.Colonia,
            Ciudad = request.Ciudad,
            Estado = request.Estado,
            CodigoPostal = request.CodigoPostal,
            Telefono = request.Telefono,
            Referencia = request.Referencia,
            EsPrincipal = request.EsPrincipal,
            IsActive = true
        };

        await dbContext.Set<ShippingAddress>().AddAsync(address, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<Guid>.Success(address.Id);
    }
}
