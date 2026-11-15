using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;

/// <param name="Stock">
/// Optional availability flag: null keeps the current availability; any value is normalized to the available /
/// sold-out stock (greater than 0 means available). Stock is never stored as an inventory count.
/// </param>
public sealed record UpdateVariantCommand(
    Guid Id,
    string Sku,
    string Nombre,
    decimal PrecioVenta,
    decimal PrecioCosto,
    decimal SizeValue,
    string SizeUnit,
    int? Stock,
    string? Barcode,
    bool IsActive,
    int SortOrder) : IRequest<Result>;
