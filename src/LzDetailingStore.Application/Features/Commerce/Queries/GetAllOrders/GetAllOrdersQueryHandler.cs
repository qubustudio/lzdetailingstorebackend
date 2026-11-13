using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Commerce.Queries.GetAllOrders;

public sealed class GetAllOrdersQueryHandler(DbContext dbContext)
    : IRequestHandler<GetAllOrdersQuery, Result<AdminOrderPageDto>>
{
    private static readonly TimeZoneInfo MexicoTimeZone = ResolveMexicoTimeZone();

    public async Task<Result<AdminOrderPageDto>> Handle(GetAllOrdersQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 100);

        // Left join: an order is still listed if its customer account no longer exists.
        var query =
            from o in dbContext.Set<Order>().AsNoTracking()
            join u in dbContext.Set<User>().AsNoTracking() on o.UserId equals u.Id into customers
            from u in customers.DefaultIfEmpty()
            select new { Order = o, Customer = u };

        if (request.Status.HasValue)
            query = query.Where(x => x.Order.Status == request.Status.Value);

        // Date range follows Mexico City calendar days; CreatedAt is stored in UTC.
        if (request.Desde.HasValue)
        {
            var fromUtc = ToUtc(request.Desde.Value);
            query = query.Where(x => x.Order.CreatedAt >= fromUtc);
        }

        if (request.Hasta.HasValue)
        {
            var toUtcExclusive = ToUtc(request.Hasta.Value.AddDays(1));
            query = query.Where(x => x.Order.CreatedAt < toUtcExclusive);
        }

        var search = NormalizeSearch(request.Search);
        if (search is not null)
        {
            query = query.Where(x =>
                x.Order.Id.ToString().Contains(search) ||
                (x.Customer != null &&
                 (x.Customer.Nombre.ToLower().Contains(search) || x.Customer.Email.ToLower().Contains(search))));
        }

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        var rows = await query
            .OrderByDescending(x => x.Order.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(x => new
            {
                x.Order.Id,
                x.Order.Status,
                CustomerName = x.Customer != null ? x.Customer.Nombre : null,
                CustomerEmail = x.Customer != null ? x.Customer.Email : null,
                x.Order.Total,
                TotalItems = x.Order.Items.Sum(i => i.Quantity),
                x.Order.CreatedAt
            })
            .ToListAsync(ct);

        var showMoney = request.IncluyeMontos;
        var items = rows
            .Select(r => new AdminOrderListItemDto(
                r.Id,
                r.Status.ToString(),
                r.CustomerName,
                r.CustomerEmail,
                showMoney ? r.Total : null,
                r.TotalItems,
                r.CreatedAt))
            .ToList();

        return new AdminOrderPageDto(showMoney, items, total, page, totalPages);
    }

    /// <summary>Trims, drops a leading '#' (order numbers are shown as #abc123) and lowercases.</summary>
    private static string? NormalizeSearch(string? search)
    {
        var term = search?.Trim().TrimStart('#').Trim();
        return string.IsNullOrEmpty(term) ? null : term.ToLowerInvariant();
    }

    private static DateTime ToUtc(DateOnly mexicoDay) =>
        TimeZoneInfo.ConvertTimeToUtc(mexicoDay.ToDateTime(TimeOnly.MinValue), MexicoTimeZone);

    private static TimeZoneInfo ResolveMexicoTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows hosts without ICU only know the Windows id.
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time (Mexico)");
        }
    }
}
