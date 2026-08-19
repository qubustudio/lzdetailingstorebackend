using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Entities.Commerce;

public sealed class Order : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid ShippingAddressId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pendiente;
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string? MercadoPagoPreferenceId { get; set; }
    public string? MercadoPagoPaymentId { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TrackingUrl { get; set; }
    public string? Notes { get; set; }

    public ShippingAddress ShippingAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
}
