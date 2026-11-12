using LzDetailingStore.Application.Features.Catalog.DTOs;

namespace LzDetailingStore.Application.Features.Intelligence.DTOs;

/// <summary>
/// Same fields as <see cref="ProductListDto"/> plus the customer-facing reason of the relation.
/// Variants expose availability only — never stock counts.
/// </summary>
public sealed record RecommendedProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal? PrecioBase,
    decimal? PrecioOriginal,
    string ImageUrl,
    string? ImageAlt,
    string? Badge,
    string BrandName,
    string CategoryName,
    IList<ProductListVariantDto> Variants,
    string? Reason);
