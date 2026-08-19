using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateVariant;

public sealed record CreateVariantCommand(
    Guid ProductId,
    string Sku,
    string Nombre,
    decimal PrecioVenta,
    decimal PrecioCosto,
    decimal SizeValue,
    string SizeUnit,
    int Stock,
    string? Barcode,
    int SortOrder) : IRequest<Result<Guid>>;
