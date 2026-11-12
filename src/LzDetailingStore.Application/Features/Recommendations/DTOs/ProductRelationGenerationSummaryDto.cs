namespace LzDetailingStore.Application.Features.Recommendations.DTOs;

public sealed record ProductRelationGenerationSummaryDto(
    string Mode,
    int SourceProducts,
    int BatchesSucceeded,
    int BatchesFailed,
    int RelationsCreated);
