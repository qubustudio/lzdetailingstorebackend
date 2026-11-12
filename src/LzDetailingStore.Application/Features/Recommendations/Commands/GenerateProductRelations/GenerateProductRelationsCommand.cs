using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.GenerateProductRelations;

/// <summary>
/// Generates Draft product relations with the AI chat client. Executed by a background job.
/// </summary>
public sealed record GenerateProductRelationsCommand(ProductRelationGenerationMode Mode)
    : IRequest<Result<ProductRelationGenerationSummaryDto>>;
