namespace LzDetailingStore.Application.Features.Admin.Dashboard.DTOs;

/// <param name="IncluyeVentas">
/// True when the caller may see money figures (<c>dashboard.ventas</c>). When false every money field is
/// null and <see cref="VentasPorCategoria"/> is empty; counts (orders, units) are always present.
/// <see cref="VentasPorMarca"/> is always returned; only its revenue is null when money is hidden.
/// </param>
public sealed record DashboardSummaryDto(
    bool IncluyeVentas,
    DashboardKpisDto Kpis,
    IReadOnlyList<DailySalesDto> VentasPorDia,
    IReadOnlyList<TopProductDto> TopProductos,
    IReadOnlyList<CategorySalesDto> VentasPorCategoria,
    IReadOnlyList<BrandSalesDto> VentasPorMarca,
    IReadOnlyList<OrdersByStatusDto> PedidosPorEstado);

public sealed record DashboardKpisDto(
    decimal? VentasMes,
    int PedidosMes,
    decimal? TicketPromedio,
    int ProductosAgotados);

/// <param name="Fecha">Mexico City calendar day, serialized as "yyyy-MM-dd".</param>
public sealed record DailySalesDto(DateOnly Fecha, decimal? Ingresos, int Pedidos);

public sealed record TopProductDto(Guid ProductoId, string Nombre, int Unidades, decimal? Ingresos);

public sealed record CategorySalesDto(string Categoria, decimal Ingresos);

public sealed record BrandSalesDto(string Marca, int Unidades, decimal? Ingresos);

public sealed record OrdersByStatusDto(string Estado, int Cantidad);
