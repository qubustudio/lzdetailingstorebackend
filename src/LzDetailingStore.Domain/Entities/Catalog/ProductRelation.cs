using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Entities.Catalog;

/// <summary>
/// Directed relation between two products ("customers viewing ProductId may also want RelatedProductId").
/// Only <see cref="ProductRelationStatus.Approved"/> relations are shown in the public storefront.
/// </summary>
public sealed class ProductRelation : BaseEntity
{
    public const int ReasonMaxLength = 300;

    public Guid ProductId { get; set; }
    public Guid RelatedProductId { get; set; }
    public ProductRelationType Type { get; set; }
    public string? Reason { get; set; }
    public ProductRelationStatus Status { get; set; } = ProductRelationStatus.Draft;
    public ProductRelationSource Source { get; set; } = ProductRelationSource.Ai;
    public int SortOrder { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }

    public Product Product { get; set; } = null!;
    public Product RelatedProduct { get; set; } = null!;

    public void Review(ProductRelationStatus status, Guid? reviewerId, DateTime reviewedAtUtc)
    {
        Status = status;
        ReviewedBy = reviewerId;
        ReviewedAt = reviewedAtUtc;
    }
}
