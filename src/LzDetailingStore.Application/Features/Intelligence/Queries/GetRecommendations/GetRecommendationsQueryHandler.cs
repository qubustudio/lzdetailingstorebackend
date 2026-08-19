using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;

public sealed class GetRecommendationsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetRecommendationsQuery, Result<List<ProductListDto>>>
{
    public async Task<Result<List<ProductListDto>>> Handle(
        GetRecommendationsQuery request, CancellationToken ct)
    {
        var product = await dbContext.Set<Product>()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, ct);

        if (product is null)
            return Result<List<ProductListDto>>.NotFound("Producto no encontrado.");

        // If no embedding, return empty (graceful degradation)
        if (product.Embedding is null || product.Embedding.Length == 0)
            return Result<List<ProductListDto>>.Success([]);

        // Use pgvector cosine distance to find similar products
        var vectorString = $"[{string.Join(",", product.Embedding)}]";

        var recommendations = await dbContext.Set<Product>()
            .FromSqlRaw(
                """
                SELECT p.* FROM products p
                WHERE p.id != {0}
                  AND p.is_active = true
                  AND p.embedding IS NOT NULL
                ORDER BY p.embedding <=> {1}::vector
                LIMIT 10
                """,
                request.ProductId, vectorString)
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .ToListAsync(ct);

        // Shuffle and take requested count (controlled randomness)
        var shuffled = recommendations.ToArray();
        Random.Shared.Shuffle(shuffled);
        var count = Math.Clamp(request.Count, 3, 6);

        var result = shuffled
            .Take(count)
            .Select(p => new ProductListDto(
                p.Id, p.Name, p.Slug, p.Description,
                p.PrecioBase, p.PrecioOriginal,
                p.ImageUrl, p.ImageAlt, p.Badge,
                p.Brand.Nombre, p.Category.Nombre))
            .ToList();

        return result;
    }
}
