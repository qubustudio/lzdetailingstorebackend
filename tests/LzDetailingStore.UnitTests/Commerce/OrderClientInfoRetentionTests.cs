using FluentAssertions;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Commerce;

/// <summary>
/// The InMemory provider has no ExecuteUpdate, so these tests exercise the load-and-save fallback of
/// <see cref="OrderClientInfoRetention.ClearExpiredAsync"/>, which shares its filter with the bulk UPDATE
/// used on PostgreSQL.
/// </summary>
public sealed class OrderClientInfoRetentionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly AdminOrderTestContext _ctx = AdminOrderTestContext.Create();

    [Fact]
    public async Task Limpieza_SoloBorraDatosDePedidosConMasDeDosAnios()
    {
        var expired = AddOrder(Now.UtcDateTime.AddYears(-2).AddDays(-1), "1.1.1.1", "UA viejo");
        var recent = AddOrder(Now.UtcDateTime.AddYears(-2).AddDays(1), "2.2.2.2", "UA reciente");
        var today = AddOrder(Now.UtcDateTime, "3.3.3.3", "UA hoy");
        await _ctx.SaveChangesAsync();

        var cleared = await OrderClientInfoRetention.ClearExpiredAsync(_ctx, Now, CancellationToken.None);

        cleared.Should().Be(1);
        _ctx.ChangeTracker.Clear();
        var orders = await _ctx.Set<Order>().ToDictionaryAsync(o => o.Id);
        orders[expired.Id].ClientIp.Should().BeNull();
        orders[expired.Id].ClientUserAgent.Should().BeNull();
        orders[recent.Id].ClientIp.Should().Be("2.2.2.2");
        orders[recent.Id].ClientUserAgent.Should().Be("UA reciente");
        orders[today.Id].ClientIp.Should().Be("3.3.3.3");
    }

    [Fact]
    public async Task Limpieza_PedidoViejoConSoloUnDato_LoBorraYNoCuentaLosYaLimpios()
    {
        var onlyUserAgent = AddOrder(Now.UtcDateTime.AddYears(-3), null, "UA");
        AddOrder(Now.UtcDateTime.AddYears(-3), null, null);
        await _ctx.SaveChangesAsync();

        var cleared = await OrderClientInfoRetention.ClearExpiredAsync(_ctx, Now, CancellationToken.None);

        cleared.Should().Be(1);
        _ctx.ChangeTracker.Clear();
        (await _ctx.Set<Order>().SingleAsync(o => o.Id == onlyUserAgent.Id)).ClientUserAgent.Should().BeNull();
        (await _ctx.Set<Order>().CountAsync()).Should().Be(2, "the orders themselves are kept");
    }

    [Fact]
    public async Task Limpieza_SegundaEjecucion_NoEncuentraNada()
    {
        AddOrder(Now.UtcDateTime.AddYears(-5), "1.1.1.1", "UA");
        await _ctx.SaveChangesAsync();
        await OrderClientInfoRetention.ClearExpiredAsync(_ctx, Now, CancellationToken.None);

        var cleared = await OrderClientInfoRetention.ClearExpiredAsync(_ctx, Now, CancellationToken.None);

        cleared.Should().Be(0);
    }

    [Fact]
    public void Corte_EsDosAniosAntesDeAhora() =>
        OrderClientInfoRetention.CutoffFor(Now).Should().Be(new DateTime(2024, 11, 15, 12, 0, 0, DateTimeKind.Utc));

    private Order AddOrder(DateTime createdAtUtc, string? ip, string? userAgent)
    {
        var order = new Order { UserId = Guid.NewGuid(), CreatedAt = createdAtUtc, ClientIp = ip, ClientUserAgent = userAgent };
        _ctx.Add(order);
        return order;
    }
}
