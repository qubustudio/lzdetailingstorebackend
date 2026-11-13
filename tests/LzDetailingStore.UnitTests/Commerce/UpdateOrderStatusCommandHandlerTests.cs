using FluentAssertions;
using LzDetailingStore.Application.Features.Commerce.Commands.UpdateOrderStatus;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.UnitTests.Recommendations;

namespace LzDetailingStore.UnitTests.Commerce;

public sealed class UpdateOrderStatusCommandHandlerTests
{
    private readonly AdminOrderTestContext _ctx = AdminOrderTestContext.Create();

    [Fact]
    public async Task Pendiente_AdminNoPuedeMarcarPagada_Devuelve400ConMensajeDeMercadoPago()
    {
        var order = await SeedAsync(OrderStatus.Pendiente);

        var result = await Handle(order.Id, OrderStatus.Pagada);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error!.Code.Should().Be("INVALID_TRANSITION");
        result.Error.Message.Should().Be("El pago solo se confirma desde Mercado Pago.");
        (await _ctx.Set<Order>().FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Pendiente);
    }

    [Theory]
    [InlineData(OrderStatus.Cancelada)]
    [InlineData(OrderStatus.Enviada)]
    [InlineData(OrderStatus.Rechazada)]
    public async Task AdminNoPuedeMarcarPagadaDesdeNingunEstado(OrderStatus from)
    {
        var order = await SeedAsync(from);

        var result = await Handle(order.Id, OrderStatus.Pagada);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("INVALID_TRANSITION");
    }

    [Fact]
    public async Task Pendiente_AdminPuedeCancelar()
    {
        var order = await SeedAsync(OrderStatus.Pendiente);

        var result = await Handle(order.Id, OrderStatus.Cancelada);

        result.IsSuccess.Should().BeTrue();
        (await _ctx.Set<Order>().FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Cancelada);
    }

    [Fact]
    public async Task Pagada_AdminPuedeMarcarEnviada()
    {
        var order = await SeedAsync(OrderStatus.Pagada);

        var result = await Handle(order.Id, OrderStatus.Enviada);

        result.IsSuccess.Should().BeTrue();
        (await _ctx.Set<Order>().FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Enviada);
    }

    [Fact]
    public async Task TransicionInvalida_Devuelve400ConMensajeGenerico()
    {
        var order = await SeedAsync(OrderStatus.Pendiente);

        var result = await Handle(order.Id, OrderStatus.Enviada);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error!.Code.Should().Be("INVALID_TRANSITION");
        result.Error.Message.Should().Be("No se puede cambiar de 'Pendiente' a 'Enviada'.");
    }

    [Fact]
    public async Task PedidoInexistente_Devuelve404()
    {
        var result = await Handle(Guid.NewGuid(), OrderStatus.Cancelada);

        result.StatusCode.Should().Be(404);
    }

    private async Task<Order> SeedAsync(OrderStatus status)
    {
        var order = new Order { UserId = Guid.NewGuid(), Status = status, Total = 100m };
        _ctx.Add(order);
        await _ctx.SaveChangesAsync();
        return order;
    }

    private Task<Result> Handle(Guid orderId, OrderStatus newStatus) =>
        new UpdateOrderStatusCommandHandler(_ctx, new PassthroughUnitOfWork(_ctx))
            .Handle(new UpdateOrderStatusCommand(orderId, newStatus), CancellationToken.None);
}
