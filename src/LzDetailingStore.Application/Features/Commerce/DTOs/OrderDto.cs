namespace LzDetailingStore.Application.Features.Commerce.DTOs;

public sealed record OrderDto(
    Guid Id,
    string Status,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string? TrackingNumber,
    string? TrackingUrl,
    DateTime CreatedAt,
    IList<OrderItemDto> Items);

public sealed record OrderItemDto(
    Guid Id,
    string ProductName,
    string VariantName,
    string Sku,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record OrderListDto(
    Guid Id,
    string Status,
    decimal Total,
    int TotalItems,
    DateTime CreatedAt);
