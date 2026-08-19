using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAddresses;

public sealed class GetAddressesQueryHandler(DbContext dbContext)
    : IRequestHandler<GetAddressesQuery, Result<List<ShippingAddressDto>>>
{
    public async Task<Result<List<ShippingAddressDto>>> Handle(GetAddressesQuery request, CancellationToken ct)
    {
        var addresses = await dbContext.Set<ShippingAddress>()
            .Where(a => a.UserId == request.UserId && a.IsActive)
            .OrderByDescending(a => a.EsPrincipal)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new ShippingAddressDto(
                a.Id, a.Nombre, a.Calle, a.NumeroExterior, a.NumeroInterior,
                a.Colonia, a.Ciudad, a.Estado, a.CodigoPostal,
                a.Telefono, a.Referencia, a.EsPrincipal))
            .ToListAsync(ct);

        return addresses;
    }
}
