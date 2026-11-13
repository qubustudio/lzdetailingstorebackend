using FluentAssertions;
using LzDetailingStore.Application.Features.Admin.Dashboard.Queries.GetDashboardSummary;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Dashboard;

public sealed class GetDashboardSummaryQueryHandlerTests
{
    // 2026-11-12 12:00 in Mexico City (UTC-6, no DST since 2022).
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 18, 0, 0, TimeSpan.Zero);

    private readonly DashboardTestContext _ctx = DashboardTestContext.Create();
    private readonly GetDashboardSummaryQueryHandler _handler;

    private readonly Category _ceras = new() { Nombre = "Ceras" };
    private readonly Category _shampoos = new() { Nombre = "Shampoos" };

    private readonly Brand _meguiars = new() { Nombre = "Meguiar's" };
    private readonly Brand _gyeon = new() { Nombre = "Gyeon" };
    private readonly Brand _koch = new() { Nombre = "Koch Chemie" };

    public GetDashboardSummaryQueryHandlerTests()
    {
        _handler = new GetDashboardSummaryQueryHandler(_ctx, new FixedTimeProvider(Now));
        _ctx.AddRange(_ceras, _shampoos);
        _ctx.AddRange(_meguiars, _gyeon, _koch);
    }

    [Fact]
    public async Task VentasPorMarca_AgrupaPorMarcaYSumaUnidadesEIngresos()
    {
        var ceraMeguiars = AddSellableProduct("Cera Meguiar's", _ceras, _meguiars);
        var shampooMeguiars = AddSellableProduct("Shampoo Meguiar's", _shampoos, _meguiars);
        var coatingGyeon = AddSellableProduct("Coating Gyeon", _ceras, _gyeon);
        AddSellableProduct("Sin ventas", _ceras, _koch);

        AddOrder(OrderStatus.Pagada, 500m, Local(2026, 11, 5, 10), (ceraMeguiars, 2, 200m), (coatingGyeon, 1, 300m));
        AddOrder(OrderStatus.Entregada, 150m, Local(2026, 11, 6, 10), (shampooMeguiars, 3, 150m));
        await _ctx.SaveChangesAsync();

        var marcas = (await Send()).VentasPorMarca;

        marcas.Should().BeEquivalentTo(
            new[]
            {
                new { Marca = "Meguiar's", Unidades = 5, Ingresos = (decimal?)350m },
                new { Marca = "Gyeon", Unidades = 1, Ingresos = (decimal?)300m }
            },
            o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task VentasPorMarca_ConVentas_OrdenaPorIngresos_SinVentas_PorUnidadesYOcultaIngresos()
    {
        var barato = AddSellableProduct("Microfibra", _ceras, _koch);
        var caro = AddSellableProduct("Coating", _ceras, _gyeon);
        AddOrder(OrderStatus.Pagada, 1100m, Local(2026, 11, 10, 10), (barato, 10, 100m), (caro, 1, 1000m));
        await _ctx.SaveChangesAsync();

        (await Send(incluyeVentas: true)).VentasPorMarca.Should().BeEquivalentTo(
            new[]
            {
                new { Marca = "Gyeon", Unidades = 1, Ingresos = (decimal?)1000m },
                new { Marca = "Koch Chemie", Unidades = 10, Ingresos = (decimal?)100m }
            },
            o => o.WithStrictOrdering());

        (await Send(incluyeVentas: false)).VentasPorMarca.Should().BeEquivalentTo(
            new[]
            {
                new { Marca = "Koch Chemie", Unidades = 10, Ingresos = (decimal?)null },
                new { Marca = "Gyeon", Unidades = 1, Ingresos = (decimal?)null }
            },
            o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task VentasPorMarca_ExcluyePedidosNoPagadosYFueraDelPeriodo()
    {
        var cera = AddSellableProduct("Cera", _ceras, _meguiars);
        var coating = AddSellableProduct("Coating", _ceras, _gyeon);
        AddOrder(OrderStatus.Enviada, 100m, Local(2026, 11, 11, 10), (cera, 1, 100m));
        foreach (var status in new[] { OrderStatus.Pendiente, OrderStatus.Cancelada, OrderStatus.Rechazada, OrderStatus.Reembolsada })
            AddOrder(status, 900m, Local(2026, 11, 11, 10), (cera, 9, 900m), (coating, 9, 900m));
        AddOrder(OrderStatus.Pagada, 900m, Local(2026, 10, 1, 10), (coating, 9, 900m));
        await _ctx.SaveChangesAsync();

        (await Send(dias: 7)).VentasPorMarca.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Marca = "Meguiar's", Unidades = 1, Ingresos = (decimal?)100m });
    }

    [Fact]
    public async Task VentasPorDia_RellenaConCerosYOrdenAscendente()
    {
        AddOrder(OrderStatus.Pagada, 150m, Local(2026, 11, 10, 9));
        AddOrder(OrderStatus.Entregada, 50m, Local(2026, 11, 10, 23));
        await _ctx.SaveChangesAsync();

        var dto = (await Send(dias: 7)).VentasPorDia;

        dto.Select(d => d.Fecha).Should().Equal(
            Enumerable.Range(6, 7).Select(day => new DateOnly(2026, 11, day)));
        dto.Single(d => d.Fecha == new DateOnly(2026, 11, 10)).Should().BeEquivalentTo(
            new { Ingresos = 200m, Pedidos = 2 });
        dto.Where(d => d.Fecha != new DateOnly(2026, 11, 10))
            .Should().OnlyContain(d => d.Ingresos == 0m && d.Pedidos == 0);
    }

    [Fact]
    public async Task SoloPedidosPagados_CuentanEnVentasTopYCategorias()
    {
        var cera = AddSellableProduct("Cera Premium", _ceras);
        var shampoo = AddSellableProduct("Shampoo pH", _shampoos);

        AddOrder(OrderStatus.Pagada, 300m, Local(2026, 11, 5, 10), (cera, 2, 200m), (shampoo, 1, 100m));
        AddOrder(OrderStatus.Enviada, 100m, Local(2026, 11, 6, 10), (shampoo, 1, 100m));
        AddOrder(OrderStatus.Entregada, 100m, Local(2026, 11, 7, 10), (cera, 1, 100m));
        foreach (var status in new[] { OrderStatus.Pendiente, OrderStatus.Cancelada, OrderStatus.Rechazada, OrderStatus.Reembolsada })
            AddOrder(status, 1000m, Local(2026, 11, 8, 10), (shampoo, 10, 1000m));
        await _ctx.SaveChangesAsync();

        var dto = await Send();

        dto.IncluyeVentas.Should().BeTrue();
        dto.Kpis.VentasMes.Should().Be(500m);
        dto.Kpis.TicketPromedio.Should().Be(166.67m);
        dto.TopProductos.Should().BeEquivalentTo(
            new[]
            {
                new { ProductoId = cera.Id, Nombre = "Cera Premium", Unidades = 3, Ingresos = (decimal?)300m },
                new { ProductoId = shampoo.Id, Nombre = "Shampoo pH", Unidades = 2, Ingresos = (decimal?)200m }
            },
            o => o.WithStrictOrdering());
        dto.VentasPorCategoria.Should().BeEquivalentTo(
            new[] { new { Categoria = "Ceras", Ingresos = 300m }, new { Categoria = "Shampoos", Ingresos = 200m } },
            o => o.WithStrictOrdering());
        dto.VentasPorDia.Sum(d => d.Ingresos).Should().Be(500m);
    }

    [Fact]
    public async Task PedidosMes_ExcluyeCanceladosRechazadosYPedidosDelMesAnteriorEnHoraDeMexico()
    {
        AddOrder(OrderStatus.Pagada, 100m, Local(2026, 11, 1, 0));
        AddOrder(OrderStatus.Pendiente, 100m, Local(2026, 11, 12, 11));
        AddOrder(OrderStatus.Cancelada, 100m, Local(2026, 11, 3, 10));
        AddOrder(OrderStatus.Rechazada, 100m, Local(2026, 11, 4, 10));
        // 2026-11-01 03:00 UTC is still October 31 in Mexico City.
        AddOrder(OrderStatus.Pagada, 999m, new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc));
        await _ctx.SaveChangesAsync();

        var kpis = (await Send()).Kpis;

        kpis.PedidosMes.Should().Be(2);
        kpis.VentasMes.Should().Be(100m);
    }

    [Fact]
    public async Task TicketPromedio_SinPedidosPagados_EsCero()
    {
        AddOrder(OrderStatus.Pendiente, 100m, Local(2026, 11, 2, 10));
        AddOrder(OrderStatus.Cancelada, 100m, Local(2026, 11, 2, 11));
        await _ctx.SaveChangesAsync();

        var kpis = (await Send()).Kpis;

        kpis.TicketPromedio.Should().Be(0m);
        kpis.VentasMes.Should().Be(0m);
    }

    [Fact]
    public async Task PedidosPorEstado_IncluyeTodosLosEstadosConCerosEnOrdenDelEnum()
    {
        AddOrder(OrderStatus.Pagada, 10m, Local(2025, 1, 1, 10));
        AddOrder(OrderStatus.Pagada, 10m, Local(2026, 11, 1, 10));
        AddOrder(OrderStatus.Reembolsada, 10m, Local(2024, 6, 1, 10));
        await _ctx.SaveChangesAsync();

        var estados = (await Send()).PedidosPorEstado;

        estados.Select(e => e.Estado).Should().Equal(Enum.GetNames<OrderStatus>());
        estados.Single(e => e.Estado == "Pagada").Cantidad.Should().Be(2);
        estados.Single(e => e.Estado == "Reembolsada").Cantidad.Should().Be(1);
        estados.Where(e => e.Estado is not ("Pagada" or "Reembolsada")).Should().OnlyContain(e => e.Cantidad == 0);
    }

    [Fact]
    public async Task ProductosAgotados_CuentaSoloActivosSinVarianteDisponible()
    {
        AddProduct("Disponible", _ceras, (true, ProductVariant.AvailableStock));
        AddProduct("Agotado", _ceras, (true, ProductVariant.UnavailableStock));
        AddProduct("Solo variante inactiva disponible", _ceras, (false, ProductVariant.AvailableStock));
        AddProduct("Sin variantes", _ceras);
        AddProduct("Mixto", _ceras, (true, ProductVariant.UnavailableStock), (true, ProductVariant.AvailableStock));
        var inactive = AddProduct("Inactivo agotado", _ceras, (true, ProductVariant.UnavailableStock));
        inactive.IsActive = false;
        await _ctx.SaveChangesAsync();

        (await Send()).Kpis.ProductosAgotados.Should().Be(3);
    }

    [Fact]
    public async Task SinPermisoDeVentas_OcultaMontosYConservaConteos()
    {
        var cera = AddSellableProduct("Cera Premium", _ceras);
        AddOrder(OrderStatus.Pagada, 300m, Local(2026, 11, 12, 9), (cera, 3, 300m));
        await _ctx.SaveChangesAsync();

        var dto = await Send(incluyeVentas: false);

        dto.IncluyeVentas.Should().BeFalse();
        dto.Kpis.VentasMes.Should().BeNull();
        dto.Kpis.TicketPromedio.Should().BeNull();
        dto.Kpis.PedidosMes.Should().Be(1);
        dto.VentasPorDia.Should().OnlyContain(d => d.Ingresos == null);
        dto.VentasPorDia.Single(d => d.Fecha == new DateOnly(2026, 11, 12)).Pedidos.Should().Be(1);
        dto.TopProductos.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Unidades = 3, Ingresos = (decimal?)null });
        dto.VentasPorCategoria.Should().BeEmpty();
    }

    [Fact]
    public async Task ConPermisoDeVentas_IncluyeMontos()
    {
        var cera = AddSellableProduct("Cera Premium", _ceras);
        AddOrder(OrderStatus.Pagada, 300m, Local(2026, 11, 12, 9), (cera, 3, 300m));
        await _ctx.SaveChangesAsync();

        var dto = await Send(incluyeVentas: true);

        dto.IncluyeVentas.Should().BeTrue();
        dto.Kpis.VentasMes.Should().Be(300m);
        dto.Kpis.TicketPromedio.Should().Be(300m);
        dto.VentasPorDia.Single(d => d.Fecha == new DateOnly(2026, 11, 12)).Ingresos.Should().Be(300m);
        dto.TopProductos.Single().Ingresos.Should().Be(300m);
        dto.VentasPorCategoria.Should().ContainSingle().Which.Ingresos.Should().Be(300m);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(30, true)]
    [InlineData(365, true)]
    [InlineData(366, false)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void Validator_AceptaSolo7a365Dias(int dias, bool valid)
    {
        var result = new GetDashboardSummaryQueryValidator().Validate(new GetDashboardSummaryQuery(dias));

        result.IsValid.Should().Be(valid);
    }

    private async Task<Application.Features.Admin.Dashboard.DTOs.DashboardSummaryDto> Send(
        int dias = 30, bool incluyeVentas = true)
    {
        var result = await _handler.Handle(new GetDashboardSummaryQuery(dias, incluyeVentas), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    /// <summary>Converts a Mexico City wall-clock time (UTC-6) to the UTC value stored in CreatedAt.</summary>
    private static DateTime Local(int year, int month, int day, int hour) =>
        new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc).AddHours(6);

    private Product AddProduct(string name, Category category, params (bool Active, int Stock)[] variants) =>
        AddProduct(name, category, _meguiars, variants);

    private Product AddProduct(
        string name, Category category, Brand brand, params (bool Active, int Stock)[] variants)
    {
        var product = new Product { Name = name, CategoryId = category.Id, BrandId = brand.Id, IsActive = true };
        foreach (var (active, stock) in variants)
            product.Variants.Add(new ProductVariant { ProductId = product.Id, IsActive = active, Stock = stock });
        _ctx.Add(product);
        return product;
    }

    private Product AddSellableProduct(string name, Category category) =>
        AddProduct(name, category, (true, ProductVariant.AvailableStock));

    private Product AddSellableProduct(string name, Category category, Brand brand) =>
        AddProduct(name, category, brand, (true, ProductVariant.AvailableStock));

    private void AddOrder(
        OrderStatus status, decimal total, DateTime createdAtUtc,
        params (Product Product, int Quantity, decimal Subtotal)[] lines)
    {
        var order = new Order { Status = status, Total = total, CreatedAt = createdAtUtc };
        foreach (var (product, quantity, subtotal) in lines)
        {
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                VariantId = product.Variants.First().Id,
                Quantity = quantity,
                UnitPrice = subtotal / quantity,
                Subtotal = subtotal,
                ProductName = product.Name
            });
        }
        _ctx.Add(order);
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

/// <summary>
/// Minimal in-memory DbContext with the commerce and catalog entities the dashboard reads,
/// avoiding the PostgreSQL-specific configuration of the real AppDbContext.
/// </summary>
public sealed class DashboardTestContext(DbContextOptions<DashboardTestContext> options) : DbContext(options)
{
    public static DashboardTestContext Create()
    {
        var options = new DbContextOptionsBuilder<DashboardTestContext>()
            .UseInMemoryDatabase($"dashboard-tests-{Guid.NewGuid()}")
            .Options;
        return new DashboardTestContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.Ignore(o => o.ShippingAddress);
            b.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
        });
        modelBuilder.Entity<OrderItem>(b => b.HasKey(i => i.Id));

        modelBuilder.Entity<Category>(b => b.HasKey(c => c.Id));
        modelBuilder.Entity<Brand>(b => b.HasKey(br => br.Id));
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasOne(p => p.Brand).WithMany(br => br.Products).HasForeignKey(p => p.BrandId);
            b.Ignore(p => p.Images);
            b.Ignore(p => p.Specs);
            b.Ignore(p => p.ApplicationSteps);
            b.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
            b.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        });
        modelBuilder.Entity<ProductVariant>(b => b.HasKey(v => v.Id));
    }
}
