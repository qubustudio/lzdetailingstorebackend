namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record ProductVariantDto(
    Guid Id,
    string Sku,
    string Nombre,
    decimal PrecioVenta,
    decimal SizeValue,
    string SizeUnit,
    bool Disponible,
    bool IsActive,
    int SortOrder);
