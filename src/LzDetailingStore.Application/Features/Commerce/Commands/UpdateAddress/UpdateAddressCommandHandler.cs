using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Commands.UpdateAddress;

public sealed class UpdateAddressCommandHandler(
    DbContext dbContext,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateAddressCommand, Result>
{
    public async Task<Result> Handle(UpdateAddressCommand request, CancellationToken ct)
    {
        var address = await dbContext.Set<ShippingAddress>()
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == request.UserId && a.IsActive, ct);

        if (address is null)
            return Result.NotFound("Dirección no encontrada.");

        // If marking as principal, unset others
        if (request.EsPrincipal && !address.EsPrincipal)
        {
            var otherPrincipal = await dbContext.Set<ShippingAddress>()
                .Where(a => a.UserId == request.UserId && a.EsPrincipal && a.Id != request.Id && a.IsActive)
                .ToListAsync(ct);

            foreach (var addr in otherPrincipal)
                addr.EsPrincipal = false;
        }

        address.Nombre = request.Nombre;
        address.Calle = request.Calle;
        address.NumeroExterior = request.NumeroExterior;
        address.NumeroInterior = request.NumeroInterior;
        address.Colonia = request.Colonia;
        address.Ciudad = request.Ciudad;
        address.Estado = request.Estado;
        address.CodigoPostal = request.CodigoPostal;
        address.Telefono = request.Telefono;
        address.Referencia = request.Referencia;
        address.EsPrincipal = request.EsPrincipal;

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
