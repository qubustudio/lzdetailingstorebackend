namespace LzDetailingStore.Application.Features.Catalog.DTOs;

public sealed record ProductListDto(
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
    string CategoryName);
