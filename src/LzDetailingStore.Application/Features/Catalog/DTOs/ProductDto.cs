namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Slug,
    string? Description,
    decimal? PrecioBase,
    decimal? PrecioOriginal,
    string ImageUrl,
    string? ImageAlt,
    decimal Rating,
    int ReviewCount,
    string? Badge,
    BrandDto Brand,
    CategoryDto Category,
    IList<ProductVariantDto> Variants,
    IList<ProductImageDto> Images,
    IList<ProductSpecDto> Specs,
    IList<ProductStepDto> ApplicationSteps);
