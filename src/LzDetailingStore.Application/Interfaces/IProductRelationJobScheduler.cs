using LzDetailingStore.Application.Features.Recommendations;

namespace LzDetailingStore.Application.Interfaces;

/// <summary>
/// Schedules the background (fire-and-forget) generation of AI product relations.
/// </summary>
public interface IProductRelationJobScheduler
{
    /// <returns>The background job identifier.</returns>
    string EnqueueGeneration(ProductRelationGenerationMode mode);
}
