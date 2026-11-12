namespace LzDetailingStore.Application.Features.Catalog.DTOs;

/// <summary>
/// Presentation of a product as shown in the public catalog listing.
/// Exposes availability only — never the stock count.
/// </summary>
public sealed record ProductListVariantDto(
    Guid Id,
    string Nombre,
    decimal PrecioVenta,
    bool Disponible);
