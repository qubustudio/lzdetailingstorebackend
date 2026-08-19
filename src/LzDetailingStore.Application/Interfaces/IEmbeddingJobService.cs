namespace LzDetailingStore.Application.Interfaces;

public interface IEmbeddingJobService
{
    Task RegenerateEmbeddingAsync(Guid productId, CancellationToken ct = default);
}
