using LzDetailingStore.Application.Features.Recommendations.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Recommendations.Queries.GetProductRelations;

public sealed class GetProductRelationsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetProductRelationsQuery, Result<PagedResult<ProductRelationAdminDto>>>
{
    public async Task<Result<PagedResult<ProductRelationAdminDto>>> Handle(
        GetProductRelationsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = dbContext.Set<ProductRelation>().AsNoTracking().AsQueryable();

        if (ProductRelationLabels.TryParseStatus(request.Estado, out var status))
            query = query.Where(r => r.Status == status);
        if (ProductRelationLabels.TryParseType(request.Tipo, out var type))
            query = query.Where(r => r.Type == type);
        if (request.ProductId.HasValue)
            query = query.Where(r => r.ProductId == request.ProductId.Value);

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        var rows = await query
            .OrderBy(r => r.Product.Name)
            .ThenBy(r => r.Type)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Id, r.ProductId, ProductName = r.Product.Name,
                r.RelatedProductId, RelatedProductName = r.RelatedProduct.Name,
                r.Type, r.Reason, r.Status, r.Source, r.SortOrder,
                r.CreatedAt, r.ReviewedAt, r.ReviewedBy
            })
            .ToListAsync(ct);

        var items = rows
            .Select(r => new ProductRelationAdminDto(
                r.Id, r.ProductId, r.ProductName, r.RelatedProductId, r.RelatedProductName,
                r.Type.ToLabel(), r.Reason, r.Status.ToLabel(), r.Source.ToLabel(), r.SortOrder,
                r.CreatedAt, r.ReviewedAt, r.ReviewedBy))
            .ToList();

        return new PagedResult<ProductRelationAdminDto>(items, total, page, totalPages);
    }
}
