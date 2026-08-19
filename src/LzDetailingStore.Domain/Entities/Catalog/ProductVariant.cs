using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal SizeValue { get; set; }
    public string SizeUnit { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Product Product { get; set; } = null!;
}
