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
            .Where(PublicCatalog.IsListed);

        if (request.BrandId.HasValue)
            query = query.Where(p => p.BrandId == request.BrandId.Value);
        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        // Price filters and sorting use the computed "desde" price the customer sees, not the stored one.
        var listed = query.SelectListed();
        if (request.MinPrice.HasValue)
            listed = listed.Where(x => x.PrecioDesde >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue)
            listed = listed.Where(x => x.PrecioDesde <= request.MaxPrice.Value);

        // Products with something to buy come first; fully sold-out ones go last, keeping the requested order.
        var byAvailability = listed.OrderByDescending(x => x.HasAvailableVariant);
        var ordered = request.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => byAvailability.ThenBy(x => x.PrecioDesde),
            "price_desc" => byAvailability.ThenByDescending(x => x.PrecioDesde),
            "name" => byAvailability.ThenBy(x => x.Product.Name),
            _ => byAvailability.ThenByDescending(x => x.Product.CreatedAt)
        };

        var total = await listed.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        var products = await ordered
            .ThenBy(x => x.Product.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new ProductListDto(
                x.Product.Id, x.Product.Name, x.Product.Slug, x.Product.Description,
                x.PrecioDesde, x.Product.PrecioOriginal,
                x.Product.ImageUrl, x.Product.ImageAlt, x.Product.Badge,
                x.Product.Brand.Nombre, x.Product.Category.Nombre,
                x.Product.Variants
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.SortOrder)
                    .ThenBy(v => v.PrecioVenta)
                    .Select(v => new ProductListVariantDto(v.Id, v.Nombre, v.PrecioVenta, v.Stock > 0))
                    .ToList()))
            .AsSplitQuery()
            .ToListAsync(ct);

        return new PagedResult<ProductListDto>(products, total, page, totalPages);
    }
}
