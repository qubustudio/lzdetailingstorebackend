using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.Extensions.AI;

namespace LzDetailingStore.Application.Features.Recommendations.Commands.EnqueueProductRelationGeneration;

public sealed class EnqueueProductRelationGenerationCommandHandler(
    IProductRelationJobScheduler scheduler,
    IChatClient? chatClient = null)
    : IRequestHandler<EnqueueProductRelationGenerationCommand, Result<EnqueueGenerationResponseDto>>
{
    public Task<Result<EnqueueGenerationResponseDto>> Handle(
        EnqueueProductRelationGenerationCommand request, CancellationToken ct)
    {
        if (chatClient is null)
            return Task.FromResult(Result<EnqueueGenerationResponseDto>.Failure(
                new Error("AI_NOT_CONFIGURED", "La generación de recomendaciones no está configurada."), 503));

        if (!ProductRelationLabels.TryParseMode(request.Mode, out var mode))
            return Task.FromResult(Result<EnqueueGenerationResponseDto>.Failure(
                new Error("INVALID_MODE", "El modo de generación no es válido.")));

        var jobId = scheduler.EnqueueGeneration(mode);
        return Task.FromResult(Result<EnqueueGenerationResponseDto>.Success(
            new EnqueueGenerationResponseDto(jobId, request.Mode.Trim().ToLowerInvariant())));
    }
}
