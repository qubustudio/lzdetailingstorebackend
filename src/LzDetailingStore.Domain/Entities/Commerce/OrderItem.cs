using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Entities.Commerce;

public sealed class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }

    // Snapshot fields — prices at time of purchase
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public Order Order { get; set; } = null!;
}
