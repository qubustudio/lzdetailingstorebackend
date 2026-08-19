using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class ProductImage : BaseEntity
{
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageAlt { get; set; }
    public int Orden { get; set; }
}
