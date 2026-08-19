using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class ProductSpec : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Orden { get; set; }
}
