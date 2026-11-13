using LzDetailingStore.Application.Features.Admin.Dashboard.DTOs;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryQueryHandler(DbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    private const int TopProductsLimit = 10;

    // An order counts as a sale once payment is confirmed and it was not reverted afterwards:
    // Pagada -> Enviada -> Entregada. Pendiente (not paid yet), Rechazada (payment rejected),
    // Cancelada and Reembolsada (money not kept) are excluded from every revenue figure.
    private static readonly OrderStatus[] PaidStatuses =
        [OrderStatus.Pagada, OrderStatus.Enviada, OrderStatus.Entregada];

    private static readonly TimeZoneInfo MexicoTimeZone = ResolveMexicoTimeZone();

    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var orders = dbContext.Set<Order>().AsNoTracking();

        // Day and month boundaries follow Mexico City time; CreatedAt is stored in UTC.
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, MexicoTimeZone));
        var firstDay = today.AddDays(-(request.Dias - 1));
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var periodStartUtc = ToUtc(firstDay);
        var periodEndUtc = ToUtc(today.AddDays(1));
        var monthStartUtc = ToUtc(monthStart);
        var monthEndUtc = ToUtc(monthStart.AddMonths(1));

        // KPIs for the current calendar month, aggregated per status in the database.
        var monthByStatus = await orders
            .Where(o => o.CreatedAt >= monthStartUtc && o.CreatedAt < monthEndUtc)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(o => o.Total) })
            .ToListAsync(ct);

        var paidMonth = monthByStatus.Where(s => PaidStatuses.Contains(s.Status)).ToList();
        var ventasMes = paidMonth.Sum(s => s.Total);
        var paidCountMes = paidMonth.Sum(s => s.Count);
        // pedidosMes counts real orders: cancelled ones and rejected payments are not orders.
        var pedidosMes = monthByStatus
            .Where(s => s.Status is not (OrderStatus.Cancelada or OrderStatus.Rechazada))
            .Sum(s => s.Count);
        var ticketPromedio = paidCountMes == 0
            ? 0m
            : Math.Round(ventasMes / paidCountMes, 2, MidpointRounding.AwayFromZero);

        var productosAgotados = await dbContext.Set<Product>()
            .AsNoTracking()
            .CountAsync(p => p.IsActive && !p.Variants.Any(v => v.IsActive && v.Stock > 0), ct);

        // Daily sales: only (CreatedAt, Total) of paid orders in the period are read; grouping by
        // Mexico-local day happens in memory because the time-zone shift is not portable SQL.
        var paidInPeriod = await orders
            .Where(o => PaidStatuses.Contains(o.Status)
                        && o.CreatedAt >= periodStartUtc && o.CreatedAt < periodEndUtc)
            .Select(o => new { o.CreatedAt, o.Total })
            .ToListAsync(ct);

        var salesByDay = paidInPeriod
            .GroupBy(o => ToMexicoDay(o.CreatedAt))
            .ToDictionary(g => g.Key, g => (Ingresos: g.Sum(o => o.Total), Pedidos: g.Count()));

        var showMoney = request.IncluyeVentas;

        var ventasPorDia = Enumerable.Range(0, request.Dias)
            .Select(offset =>
            {
                var day = firstDay.AddDays(offset);
                var (ingresos, pedidos) = salesByDay.GetValueOrDefault(day);
                return new DailySalesDto(day, showMoney ? ingresos : null, pedidos);
            })
            .ToList();

        // Line items of paid orders in the period, using the stored purchase-time quantity/subtotal.
        var paidLines =
            from item in dbContext.Set<OrderItem>().AsNoTracking()
            join order in orders on item.OrderId equals order.Id
            join variant in dbContext.Set<ProductVariant>().AsNoTracking() on item.VariantId equals variant.Id
            join product in dbContext.Set<Product>().AsNoTracking() on variant.ProductId equals product.Id
            where PaidStatuses.Contains(order.Status)
                  && order.CreatedAt >= periodStartUtc && order.CreatedAt < periodEndUtc
            select new
            {
                ProductId = product.Id,
                ProductName = product.Name,
                product.CategoryId,
                product.BrandId,
                item.Quantity,
                item.Subtotal
            };

        var topRows = await paidLines
            .GroupBy(l => new { l.ProductId, l.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                Units = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.Subtotal)
            })
            .OrderByDescending(p => p.Units)
            .ThenByDescending(p => p.Revenue)
            .Take(TopProductsLimit)
            .ToListAsync(ct);

        var topProductos = topRows
            .Select(p => new TopProductDto(p.ProductId, p.ProductName, p.Units, showMoney ? p.Revenue : null))
            .ToList();

        // Category revenue is pure money: not even queried when the caller cannot see sales.
        List<CategorySalesDto> ventasPorCategoria = showMoney
            ? await (
                    from line in paidLines
                    join category in dbContext.Set<Category>().AsNoTracking() on line.CategoryId equals category.Id
                    group line.Subtotal by new { category.Id, category.Nombre } into g
                    select new { g.Key.Nombre, Revenue = g.Sum() })
                .OrderByDescending(c => c.Revenue)
                .Select(c => new CategorySalesDto(c.Nombre, c.Revenue))
                .ToListAsync(ct)
            : [];

        // Brand sales are always returned (units are not money); only revenue is hidden without permission.
        var brandRows =
            from line in paidLines
            join brand in dbContext.Set<Brand>().AsNoTracking() on line.BrandId equals brand.Id
            group line by new { brand.Id, brand.Nombre } into g
            select new
            {
                g.Key.Nombre,
                Units = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.Subtotal)
            };

        var orderedBrandRows = showMoney
            ? brandRows.OrderByDescending(b => b.Revenue).ThenByDescending(b => b.Units)
            : brandRows.OrderByDescending(b => b.Units);

        var brandSales = await orderedBrandRows.ThenBy(b => b.Nombre).ToListAsync(ct);

        var ventasPorMarca = brandSales
            .Select(b => new BrandSalesDto(b.Nombre, b.Units, showMoney ? b.Revenue : null))
            .ToList();

        // Status distribution over ALL orders, zero-filled and in enum declaration order.
        var countsByStatus = await orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(s => s.Status, s => s.Count, ct);

        var pedidosPorEstado = Enum.GetValues<OrderStatus>()
            .Select(s => new OrdersByStatusDto(s.ToString(), countsByStatus.GetValueOrDefault(s)))
            .ToList();

        return new DashboardSummaryDto(
            showMoney,
            new DashboardKpisDto(
                showMoney ? ventasMes : null,
                pedidosMes,
                showMoney ? ticketPromedio : null,
                productosAgotados),
            ventasPorDia,
            topProductos,
            ventasPorCategoria,
            ventasPorMarca,
            pedidosPorEstado);
    }

    private static DateTime ToUtc(DateOnly mexicoDay) =>
        TimeZoneInfo.ConvertTimeToUtc(mexicoDay.ToDateTime(TimeOnly.MinValue), MexicoTimeZone);

    private static DateOnly ToMexicoDay(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), MexicoTimeZone));

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
