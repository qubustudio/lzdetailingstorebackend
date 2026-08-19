namespace LzDetailingStore.Application.Interfaces;

public interface ISlugGenerator
{
    Task<string> GenerateAsync(string input, string entityTable, CancellationToken ct = default);
}
