namespace LzDetailingStore.Application.Features.Recommendations.DTOs;

public sealed record ProductRelationAdminDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid RelatedProductId,
    string RelatedProductName,
    string Tipo,
    string? Reason,
    string Status,
    string Source,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    Guid? ReviewedBy);
