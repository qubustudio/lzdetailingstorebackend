namespace LzDetailingStore.Application.Features.Commerce.DTOs;

public sealed record ShippingAddressDto(
    Guid Id,
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
    bool EsPrincipal);
