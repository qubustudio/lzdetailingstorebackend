using FluentAssertions;
using LzDetailingStore.Application.Features.Commerce.DTOs;
using LzDetailingStore.Application.Features.Commerce.Queries.GetAdminOrderById;
using LzDetailingStore.Application.Features.Commerce.Queries.GetAllOrders;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.Domain.Entities.Identity;
using LzDetailingStore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Commerce;

public sealed class AdminOrderQueriesTests
{
    private readonly AdminOrderTestContext _ctx = AdminOrderTestContext.Create();

    private readonly User _ana = new() { Nombre = "Ana López", Email = "ana@correo.com", Telefono = "5512345678" };
    private readonly User _beto = new() { Nombre = "Beto Ruiz", Email = "beto@empresa.mx" };

    public AdminOrderQueriesTests()
    {
        _ctx.AddRange(_ana, _beto);
    }

    // ---- List: money visibility ----

    [Fact]
    public async Task Lista_ConPermisoMontos_IncluyeTotalYBandera()
    {
        AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 10), total: 350m, (2, 100m), (1, 150m));
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAllOrdersQuery(IncluyeMontos: true));

        page.IncluyeMontos.Should().BeTrue();
        page.Data.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Status = "Pagada",
            CustomerName = "Ana López",
            CustomerEmail = "ana@correo.com",
            Total = (decimal?)350m,
            TotalItems = 3
        });
    }

    [Fact]
    public async Task Lista_SinPermisoMontos_TotalNuloYBanderaFalsa()
    {
        AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 10), total: 350m, (2, 100m), (1, 150m));
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAllOrdersQuery());

        page.IncluyeMontos.Should().BeFalse();
        var item = page.Data.Should().ContainSingle().Subject;
        item.Total.Should().BeNull();
        item.TotalItems.Should().Be(3);
        item.CustomerName.Should().Be("Ana López");
    }

    // ---- List: filters ----

    [Fact]
    public async Task Lista_FiltraPorEstado()
    {
        AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 10));
        AddOrder(_ana, OrderStatus.Enviada, Local(2026, 11, 10, 11));
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAllOrdersQuery(Status: OrderStatus.Enviada));

        page.Data.Should().ContainSingle().Which.Status.Should().Be("Enviada");
        page.Total.Should().Be(1);
    }

    [Fact]
    public async Task Lista_FiltraPorRangoDeFechasEnHoraDeMexico_Inclusivo()
    {
        // 2026-11-01 03:00 UTC is still October 31 in Mexico City.
        AddOrder(_ana, OrderStatus.Pagada, new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc));
        var first = AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 1, 0));
        var last = AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 5, 23));
        AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 6, 0));
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAllOrdersQuery(
            Desde: new DateOnly(2026, 11, 1), Hasta: new DateOnly(2026, 11, 5)));

        page.Data.Select(o => o.Id).Should().Equal(last.Id, first.Id);
        page.Total.Should().Be(2);
    }

    [Theory]
    [InlineData("ANA LÓ")]
    [InlineData("empresa.mx")]
    [InlineData("  beto  ")]
    public async Task Lista_BuscaPorNombreOEmailSinDistinguirMayusculas(string search)
    {
        AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 10));
        AddOrder(_beto, OrderStatus.Pagada, Local(2026, 11, 10, 11));
        await _ctx.SaveChangesAsync();

        var expected = search.Contains("LÓ") ? "Ana López" : "Beto Ruiz";

        var page = await List(new GetAllOrdersQuery(Search: search));

        page.Data.Should().ContainSingle().Which.CustomerName.Should().Be(expected);
    }

    [Fact]
    public async Task Lista_BuscaPorNumeroDePedido_ConOSinGato()
    {
        var target = AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 10));
        AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 11));
        await _ctx.SaveChangesAsync();
        var shortNumber = target.Id.ToString()[..8].ToUpperInvariant();

        (await List(new GetAllOrdersQuery(Search: shortNumber))).Data
            .Should().ContainSingle().Which.Id.Should().Be(target.Id);
        (await List(new GetAllOrdersQuery(Search: "#" + shortNumber))).Data
            .Should().ContainSingle().Which.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Lista_PedidoDeUsuarioInexistente_SeListaSinCliente()
    {
        var order = new Order { UserId = Guid.NewGuid(), Status = OrderStatus.Pendiente, CreatedAt = Local(2026, 11, 10, 10) };
        _ctx.Add(order);
        await _ctx.SaveChangesAsync();

        var item = (await List(new GetAllOrdersQuery())).Data.Should().ContainSingle().Subject;

        item.CustomerName.Should().BeNull();
        item.CustomerEmail.Should().BeNull();
    }

    [Fact]
    public async Task Lista_PaginaYOrdenaDelMasRecienteAlMasAntiguo()
    {
        for (var day = 1; day <= 5; day++)
            AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, day, 10));
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAllOrdersQuery(Page: 2, Limit: 2));

        page.Total.Should().Be(5);
        page.TotalPages.Should().Be(3);
        page.Page.Should().Be(2);
        page.Data.Select(o => o.CreatedAt).Should().Equal(Local(2026, 11, 3, 10), Local(2026, 11, 2, 10));
    }

    [Theory]
    [InlineData("2026-11-05", "2026-11-01", false)]
    [InlineData("2026-11-01", "2026-11-01", true)]
    [InlineData("2026-11-01", "2026-11-05", true)]
    public void Validator_HastaNoPuedeSerAnteriorADesde(string desde, string hasta, bool valid)
    {
        var query = new GetAllOrdersQuery(Desde: DateOnly.Parse(desde), Hasta: DateOnly.Parse(hasta));

        new GetAllOrdersQueryValidator().Validate(query).IsValid.Should().Be(valid);
    }

    [Fact]
    public void Validator_RechazaBusquedaDemasiadoLarga()
    {
        var query = new GetAllOrdersQuery(Search: new string('a', GetAllOrdersQuery.SearchMaxLength + 1));

        new GetAllOrdersQueryValidator().Validate(query).IsValid.Should().BeFalse();
    }

    // ---- Detail ----

    [Fact]
    public async Task Detalle_ConPermisoMontos_IncluyeMontosClienteDireccionYTransiciones()
    {
        var address = AddAddress(_ana);
        var order = AddOrder(_ana, OrderStatus.Pagada, Local(2026, 11, 10, 10), total: 349m, (2, 100m), (1, 150m));
        order.Subtotal = 250m;
        order.ShippingCost = 99m;
        order.ShippingAddressId = address.Id;
        order.TrackingNumber = "TRK1";
        await _ctx.SaveChangesAsync();

        var dto = await Detail(order.Id, incluyeMontos: true);

        dto.IncluyeMontos.Should().BeTrue();
        dto.Status.Should().Be("Pagada");
        dto.AllowedTransitions.Should().Equal("Enviada", "Cancelada");
        dto.Subtotal.Should().Be(250m);
        dto.ShippingCost.Should().Be(99m);
        dto.Total.Should().Be(349m);
        dto.TrackingNumber.Should().Be("TRK1");
        dto.Customer.Should().BeEquivalentTo(new AdminOrderCustomerDto(_ana.Id, "Ana López", "ana@correo.com", "5512345678"));
        dto.ShippingAddress!.Calle.Should().Be("Av. Reforma");
        dto.Items.Should().HaveCount(2);
        dto.Items.Should().OnlyContain(i => i.UnitPrice != null && i.Subtotal != null);
        dto.Items.Sum(i => i.Subtotal).Should().Be(250m);
    }

    [Fact]
    public async Task Detalle_SinPermisoMontos_TodosLosMontosNulos()
    {
        var order = AddOrder(_ana, OrderStatus.Enviada, Local(2026, 11, 10, 10), total: 449m, (2, 100m));
        order.Subtotal = 200m;
        order.ShippingCost = 99m;
        await _ctx.SaveChangesAsync();

        var dto = await Detail(order.Id, incluyeMontos: false);

        dto.IncluyeMontos.Should().BeFalse();
        dto.Subtotal.Should().BeNull();
        dto.ShippingCost.Should().BeNull();
        dto.Total.Should().BeNull();
        dto.AllowedTransitions.Should().Equal("Entregada");
        var item = dto.Items.Should().ContainSingle().Subject;
        item.UnitPrice.Should().BeNull();
        item.Subtotal.Should().BeNull();
        item.Quantity.Should().Be(2);
        item.ProductName.Should().NotBeNullOrEmpty();
        dto.Customer!.Email.Should().Be("ana@correo.com");
    }

    [Fact]
    public async Task Detalle_Pendiente_SoloPermiteCancelar_PagadaSoloViaMercadoPago()
    {
        var order = AddOrder(_ana, OrderStatus.Pendiente, Local(2026, 11, 10, 10));
        await _ctx.SaveChangesAsync();

        var dto = await Detail(order.Id, incluyeMontos: true);

        dto.AllowedTransitions.Should().Equal("Cancelada");
    }

    [Fact]
    public async Task Detalle_PedidoInexistente_DevuelveNotFound()
    {
        var result = await new GetAdminOrderByIdQueryHandler(_ctx)
            .Handle(new GetAdminOrderByIdQuery(Guid.NewGuid(), true), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
    }

    // ---- Helpers ----

    private async Task<AdminOrderPageDto> List(GetAllOrdersQuery query)
    {
        var result = await new GetAllOrdersQueryHandler(_ctx).Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    private async Task<AdminOrderDetailDto> Detail(Guid id, bool incluyeMontos)
    {
        var result = await new GetAdminOrderByIdQueryHandler(_ctx)
            .Handle(new GetAdminOrderByIdQuery(id, incluyeMontos), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    /// <summary>Converts a Mexico City wall-clock time (UTC-6) to the UTC value stored in CreatedAt.</summary>
    private static DateTime Local(int year, int month, int day, int hour) =>
        new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc).AddHours(6);

    private ShippingAddress AddAddress(User user)
    {
        var address = new ShippingAddress
        {
            UserId = user.Id, Nombre = "Casa", Calle = "Av. Reforma", NumeroExterior = "10",
            Colonia = "Centro", Ciudad = "CDMX", Estado = "CDMX", CodigoPostal = "06000"
        };
        _ctx.Add(address);
        return address;
    }

    private Order AddOrder(
        User user, OrderStatus status, DateTime createdAtUtc, decimal total = 100m,
        params (int Quantity, decimal Subtotal)[] lines)
    {
        var order = new Order { UserId = user.Id, Status = status, Total = total, CreatedAt = createdAtUtc };
        var n = 0;
        foreach (var (quantity, subtotal) in lines)
        {
            n++;
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                VariantId = Guid.NewGuid(),
                Quantity = quantity,
                UnitPrice = subtotal / quantity,
                Subtotal = subtotal,
                ProductName = $"Producto {n}",
                VariantName = "500 ml",
                Sku = $"SKU-{n}"
            });
        }
        _ctx.Add(order);
        return order;
    }
}

/// <summary>
/// Minimal in-memory DbContext with the entities the admin order queries read, avoiding the
/// PostgreSQL-specific configuration of the real AppDbContext.
/// </summary>
public sealed class AdminOrderTestContext(DbContextOptions<AdminOrderTestContext> options) : DbContext(options)
{
    public static AdminOrderTestContext Create()
    {
        var options = new DbContextOptionsBuilder<AdminOrderTestContext>()
            .UseInMemoryDatabase($"admin-order-tests-{Guid.NewGuid()}")
            .Options;
        return new AdminOrderTestContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.HasOne(o => o.ShippingAddress).WithMany().HasForeignKey(o => o.ShippingAddressId);
            b.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
        });
        modelBuilder.Entity<OrderItem>(b => b.HasKey(i => i.Id));
        modelBuilder.Entity<ShippingAddress>(b => b.HasKey(a => a.Id));
        modelBuilder.Entity<User>(b =>
        {
            b.HasKey(u => u.Id);
            b.Ignore(u => u.UserRoles);
            b.Ignore(u => u.OAuthAccounts);
            b.Ignore(u => u.RefreshTokens);
        });
    }
}
