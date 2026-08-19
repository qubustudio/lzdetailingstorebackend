using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Jobs;

public sealed class RegenerateEmbeddingJob(
    IServiceScopeFactory scopeFactory,
    IEmbeddingService embeddingService,
    ILogger<RegenerateEmbeddingJob> logger) : IEmbeddingJobService
{
    public Task RegenerateEmbeddingAsync(Guid productId, CancellationToken ct = default)
        => ExecuteAsync(productId, ct);

    public async Task ExecuteAsync(Guid productId, CancellationToken ct = default)
    {
        if (!embeddingService.IsAvailable)
        {
            logger.LogWarning("Embedding service unavailable, skipping for product {ProductId}", productId);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = await dbContext.Set<Product>()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specs)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
        {
            logger.LogWarning("Product {ProductId} not found for embedding generation", productId);
            return;
        }

        var textParts = new List<string>
        {
            product.Name,
            product.Brand.Nombre,
            product.Category.Nombre
        };

        if (!string.IsNullOrWhiteSpace(product.Description))
            textParts.Add(product.Description);

        foreach (var spec in product.Specs)
            textParts.Add($"{spec.Label}: {spec.Value}");

        var text = string.Join(" ", textParts);

        var embedding = await embeddingService.GenerateEmbeddingAsync(text, ct);

        if (embedding is null)
        {
            logger.LogWarning("Failed to generate embedding for product {ProductId}", productId);
            return;
        }

        var vectorString = $"[{string.Join(",", embedding)}]";
        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE products SET embedding = @p0::vector WHERE id = @p1",
            [vectorString, productId], ct);

        logger.LogInformation("Embedding generated for product {ProductId} ({Dimensions}d)",
            productId, embedding.Length);
    }
}
