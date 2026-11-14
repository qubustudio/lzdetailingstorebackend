using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProducts;

public sealed class GetAdminProductsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetAdminProductsQuery, Result<AdminProductPageDto>>
{
    public async Task<Result<AdminProductPageDto>> Handle(GetAdminProductsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 100);

        var query = dbContext.Set<Product>().AsNoTracking();

        if (request.Activo.HasValue)
            query = query.Where(p => p.IsActive == request.Activo.Value);
        if (request.BrandId.HasValue)
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        var search = request.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Sku.ToLower().Contains(search) ||
                p.Variants.Any(v => v.Sku.ToLower().Contains(search)));
        }

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        // Stock is an availability flag: only the count of sold-out variants leaves the database, never the value.
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new AdminProductListItemDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Sku,
                p.ImageUrl,
                p.Brand.Nombre,
                p.Category.Nombre,
                p.IsActive,
                p.PrecioBase,
                p.Variants.Count(v => v.IsActive),
                p.Variants.Count(v => v.IsActive && v.Stock <= 0)))
            .ToListAsync(ct);

        return new AdminProductPageDto(request.IncluyeCostos, items, total, page, totalPages);
    }
}
