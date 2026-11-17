using System.Linq.Expressions;
using LzDetailingStore.Domain.Entities.Commerce;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Infrastructure.Jobs;

/// <summary>
/// Retention rule for the buyer's IP and User-Agent stored on orders: once an order is older than
/// <see cref="RetentionYears"/>, both values are set to null. The order itself is kept.
/// </summary>
public static class OrderClientInfoRetention
{
    /// <summary>How long client metadata is kept after the order was created. Single source of truth.</summary>
    public const int RetentionYears = 2;

    /// <summary>Orders created before this instant have expired client metadata.</summary>
    public static DateTime CutoffFor(DateTimeOffset now) => now.UtcDateTime.AddYears(-RetentionYears);

    /// <summary>Orders past the cutoff that still hold an IP or a User-Agent.</summary>
    public static Expression<Func<Order, bool>> HasExpiredClientInfo(DateTime cutoffUtc) =>
        o => o.CreatedAt < cutoffUtc && (o.ClientIp != null || o.ClientUserAgent != null);

    /// <summary>
    /// Clears the client metadata of expired orders and returns how many orders were cleared.
    /// On relational providers (PostgreSQL) it is a single bulk UPDATE that does not touch UpdatedAt.
    /// Providers without bulk updates (EF InMemory, used by unit tests) get an equivalent load-and-save
    /// fallback that shares the same filter.
    /// </summary>
    public static async Task<int> ClearExpiredAsync(DbContext dbContext, DateTimeOffset now, CancellationToken ct)
    {
        var expired = dbContext.Set<Order>().Where(HasExpiredClientInfo(CutoffFor(now)));

        if (dbContext.Database.IsRelational())
        {
            return await expired.ExecuteUpdateAsync(s => s
                .SetProperty(o => o.ClientIp, (string?)null)
                .SetProperty(o => o.ClientUserAgent, (string?)null), ct);
        }

        var orders = await expired.ToListAsync(ct);
        foreach (var order in orders)
        {
            order.ClientIp = null;
            order.ClientUserAgent = null;
        }
        await dbContext.SaveChangesAsync(ct);
        return orders.Count;
    }
}
