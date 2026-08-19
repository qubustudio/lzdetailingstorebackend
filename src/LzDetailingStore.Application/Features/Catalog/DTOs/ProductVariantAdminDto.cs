namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record ProductVariantAdminDto(
    Guid Id,
    string Sku,
    string Nombre,
    decimal PrecioVenta,
    decimal PrecioCosto,
    decimal SizeValue,
    string SizeUnit,
    int Stock,
    string? Barcode,
    bool IsActive,
    int SortOrder);
