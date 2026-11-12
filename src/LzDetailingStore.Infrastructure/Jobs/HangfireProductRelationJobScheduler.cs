using Hangfire;
using LzDetailingStore.Application.Features.Recommendations;
using LzDetailingStore.Application.Interfaces;

namespace LzDetailingStore.Infrastructure.Jobs;

public sealed class HangfireProductRelationJobScheduler(IBackgroundJobClient backgroundJobClient)
    : IProductRelationJobScheduler
{
    public string EnqueueGeneration(ProductRelationGenerationMode mode) =>
        backgroundJobClient.Enqueue<GenerateProductRelationsJob>(job => job.ExecuteAsync(mode, CancellationToken.None));
}
