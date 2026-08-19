using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetOrders;

public sealed class GetOrdersQueryHandler(DbContext dbContext)
    : IRequestHandler<GetOrdersQuery, Result<PagedResult<OrderListDto>>>
{
    public async Task<Result<PagedResult<OrderListDto>>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 50);

        var query = dbContext.Set<Order>()
            .Where(o => o.UserId == request.UserId)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        var orders = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(o => o.Items)
            .Select(o => new OrderListDto(
                o.Id,
                o.Status.ToString(),
                o.Total,
                o.Items.Sum(i => i.Quantity),
                o.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<OrderListDto>(orders, total, page, totalPages);
    }
}
