using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Catalog;

public sealed class ProductVariant : BaseEntity
{
    /// <summary>
    /// Stock works as an availability flag, not an inventory count: the store has no link to
    /// the physical inventory, so a variant is either available or sold out / on request.
    /// </summary>
    public const int AvailableStock = 99;

    /// <summary>Stock value that marks a variant as sold out / on request.</summary>
    public const int UnavailableStock = 0;

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

    /// <summary>Marks the variant as available or sold out by setting the stock availability flag.</summary>
    public void SetAvailability(bool available) =>
        Stock = available ? AvailableStock : UnavailableStock;
}
