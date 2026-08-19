namespace LzDetailingStore.Application.Interfaces;

public interface IS3Service
{
    Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
