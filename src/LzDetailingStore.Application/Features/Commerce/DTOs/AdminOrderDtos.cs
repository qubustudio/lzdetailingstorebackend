namespace LzDetailingStore.Application.Features.Commerce.DTOs;

/// <summary>Permissions that shape the admin order responses.</summary>
public static class AdminOrderPermissions
{
    /// <summary>
    /// Unlocks money fields (prices, subtotals, shipping, totals) in the admin order list and detail.
    /// Without it every money field is null and <c>incluyeMontos</c> is false (Operativo).
    /// </summary>
    public const string Montos = "ordenes.montos";
}

/// <summary>
/// Paged admin order list. Same paging fields as <c>PagedResult</c>, plus <see cref="IncluyeMontos"/>:
/// when false every money field of the items is null.
/// </summary>
public sealed record AdminOrderPageDto(
    bool IncluyeMontos,
    IReadOnlyList<AdminOrderListItemDto> Data,
    int Total,
    int Page,
    int TotalPages);

public sealed record AdminOrderListItemDto(
    Guid Id,
    string Status,
    string? CustomerName,
    string? CustomerEmail,
    decimal? Total,
    int TotalItems,
    DateTime CreatedAt);

/// <summary>Admin order detail. When <see cref="IncluyeMontos"/> is false every money field is null.</summary>
public sealed record AdminOrderDetailDto(
    bool IncluyeMontos,
    Guid Id,
    string Status,
    IReadOnlyList<string> AllowedTransitions,
    AdminOrderCustomerDto? Customer,
    ShippingAddressDto? ShippingAddress,
    decimal? Subtotal,
    decimal? ShippingCost,
    decimal? Total,
    string? TrackingNumber,
    string? TrackingUrl,
    string? Notes,
    string? PaymentId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<AdminOrderItemDto> Items);

public sealed record AdminOrderCustomerDto(
    Guid Id,
    string Nombre,
    string Email,
    string? Telefono);

public sealed record AdminOrderItemDto(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? ImageUrl,
    decimal? UnitPrice,
    int Quantity,
    decimal? Subtotal);
