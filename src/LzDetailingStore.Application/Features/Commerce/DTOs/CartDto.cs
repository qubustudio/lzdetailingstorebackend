namespace LzDetailingStore.Application.Features.Commerce.DTOs;

public sealed record CartDto(
    Guid Id,
    IList<CartItemDto> Items,
    decimal Subtotal,
    int TotalItems);

public sealed record CartItemDto(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string VariantName,
    string Sku,
    string? ImageUrl,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal,
    int StockAvailable);
