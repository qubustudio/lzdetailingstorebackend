using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;

public sealed record UpdateVariantCommand(
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
    int SortOrder) : IRequest<Result>;
