using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.EnqueueProductRelationGeneration;

/// <param name="Mode">"nuevos" or "completar".</param>
public sealed record EnqueueProductRelationGenerationCommand(string Mode)
    : IRequest<Result<EnqueueGenerationResponseDto>>;
