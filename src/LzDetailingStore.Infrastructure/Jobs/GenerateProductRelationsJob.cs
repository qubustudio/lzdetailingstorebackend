using LzDetailingStore.Application.Features.Recommendations;
using LzDetailingStore.Application.Features.Recommendations.Commands.GenerateProductRelations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Jobs;

/// <summary>
/// Hangfire fire-and-forget job that generates AI product relations (not recurring).
/// </summary>
public sealed class GenerateProductRelationsJob(
    IServiceScopeFactory scopeFactory,
    ILogger<GenerateProductRelationsJob> logger)
{
    public async Task ExecuteAsync(ProductRelationGenerationMode mode, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new GenerateProductRelationsCommand(mode), ct);

        if (result.IsFailure)
        {
            logger.LogWarning("GenerateProductRelationsJob ({Mode}) did not run: {Error}",
                mode, result.Error?.Message);
            return;
        }

        var summary = result.Value!;
        logger.LogInformation(
            "GenerateProductRelationsJob ({Mode}): {Sources} sources, {Ok} batches ok, {Failed} failed, {Created} relations created",
            mode, summary.SourceProducts, summary.BatchesSucceeded, summary.BatchesFailed, summary.RelationsCreated);
    }
}
