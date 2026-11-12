using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;

namespace LzDetailingStore.Application.Features.Recommendations;

public static class ProductRelationMapper
{
    /// <summary>Requires <see cref="ProductRelation.Product"/> and <see cref="ProductRelation.RelatedProduct"/> loaded.</summary>
    public static ProductRelationAdminDto ToAdminDto(ProductRelation r) => new(
        r.Id, r.ProductId, r.Product.Name, r.RelatedProductId, r.RelatedProduct.Name,
        r.Type.ToLabel(), r.Reason, r.Status.ToLabel(), r.Source.ToLabel(), r.SortOrder,
        r.CreatedAt, r.ReviewedAt, r.ReviewedBy);
}
