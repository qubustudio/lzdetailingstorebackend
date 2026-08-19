using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Commerce.Commands.CreateAddress;

public sealed record CreateAddressCommand(
    Guid UserId,
    string Nombre,
    string Calle,
    string NumeroExterior,
    string? NumeroInterior,
    string Colonia,
    string Ciudad,
    string Estado,
    string CodigoPostal,
    string? Telefono,
    string? Referencia,
    bool EsPrincipal) : IRequest<Result<Guid>>;
