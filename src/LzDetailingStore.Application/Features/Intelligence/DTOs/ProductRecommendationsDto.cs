namespace LzDetailingStore.Application.Features.Intelligence.DTOs;

public sealed record ProductRecommendationsDto(
    IReadOnlyList<RecommendedProductDto> Complementarios,
    IReadOnlyList<RecommendedProductDto> Similares);
