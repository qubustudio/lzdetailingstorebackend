using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Application.Features.Intelligence.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;

/// <summary>
/// Returns the curated (approved) relations of a product, split by type.
/// Only related products that are visible (active with at least one active variant in stock) are returned.
/// </summary>
public sealed class GetRecommendationsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetRecommendationsQuery, Result<ProductRecommendationsDto>>
{
    private const int MaxPerType = 6;

    public async Task<Result<ProductRecommendationsDto>> Handle(
        GetRecommendationsQuery request, CancellationToken ct)
    {
        var productExists = await dbContext.Set<Product>()
            .AnyAsync(p => p.Id == request.ProductId && p.IsActive, ct);

        if (!productExists)
            return Result<ProductRecommendationsDto>.NotFound("Producto no encontrado.");

        var count = Math.Clamp(request.Count, 1, MaxPerType);

        var rows = await dbContext.Set<ProductRelation>()
            .Where(r => r.ProductId == request.ProductId
                        && r.Status == ProductRelationStatus.Approved
                        && r.RelatedProduct.IsActive
                        && r.RelatedProduct.Variants.Any(v => v.IsActive && v.Stock > 0))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new
            {
                r.Type,
                Product = new RecommendedProductDto(
                    r.RelatedProduct.Id, r.RelatedProduct.Name, r.RelatedProduct.Slug, r.RelatedProduct.Description,
                    r.RelatedProduct.PrecioBase, r.RelatedProduct.PrecioOriginal,
                    r.RelatedProduct.ImageUrl, r.RelatedProduct.ImageAlt, r.RelatedProduct.Badge,
                    r.RelatedProduct.Brand.Nombre, r.RelatedProduct.Category.Nombre,
                    r.RelatedProduct.Variants
                        .Where(v => v.IsActive)
                        .OrderBy(v => v.SortOrder)
                        .ThenBy(v => v.PrecioVenta)
                        .Select(v => new ProductListVariantDto(v.Id, v.Nombre, v.PrecioVenta, v.Stock > 0))
                        .ToList(),
                    r.Reason)
            })
            .AsSplitQuery()
            .ToListAsync(ct);

        List<RecommendedProductDto> Take(ProductRelationType type) => rows
            .Where(r => r.Type == type)
            .Select(r => r.Product)
            .Take(count)
            .ToList();

        return new ProductRecommendationsDto(
            Take(ProductRelationType.Complementary),
            Take(ProductRelationType.Similar));
    }
}
