using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class Product : AuditableEntity
{
    public Guid BrandId { get; set; }
    public Guid CategoryId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? PrecioBase { get; set; }
    public decimal? PrecioOriginal { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageAlt { get; set; }
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public string? Badge { get; set; }
    public bool IsActive { get; set; } = true;
    public float[]? Embedding { get; set; }

    public Brand Brand { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductSpec> Specs { get; set; } = [];
    public ICollection<ProductApplicationStep> ApplicationSteps { get; set; } = [];

    public void RecalculatePrecioBase()
    {
        var activeVariants = Variants.Where(v => v.IsActive).ToList();
        PrecioBase = activeVariants.Count != 0
            ? activeVariants.Min(v => v.PrecioVenta)
            : null;
    }
}
