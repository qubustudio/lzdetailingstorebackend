using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetProducts;

public sealed class GetProductsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductListDto>>>
{
    public async Task<Result<PagedResult<ProductListDto>>> Handle(
        GetProductsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = dbContext.Set<Product>()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (request.BrandId.HasValue)
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (request.MinPrice.HasValue)
            query = query.Where(p => p.PrecioBase >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.PrecioBase <= request.MaxPrice.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        query = request.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.PrecioBase),
            "price_desc" => query.OrderByDescending(p => p.PrecioBase),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        var products = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new ProductListDto(
                p.Id, p.Name, p.Slug, p.Description,
                p.PrecioBase, p.PrecioOriginal,
                p.ImageUrl, p.ImageAlt, p.Badge,
                p.Brand.Nombre, p.Category.Nombre))
            .ToListAsync(ct);

        return new PagedResult<ProductListDto>(products, total, page, totalPages);
    }
}
