namespace LzDetailingStore.Application.Interfaces;

public interface IEmbeddingService
{
    bool IsAvailable { get; }
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}
