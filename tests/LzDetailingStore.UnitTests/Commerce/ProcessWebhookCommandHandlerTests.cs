using FluentAssertions;
using LzDetailingStore.Application.Features.Commerce.Commands.ProcessWebhook;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LzDetailingStore.UnitTests.Commerce;

public sealed class ProcessWebhookCommandHandlerTests
{
    private readonly IMercadoPagoService _mercadoPago = Substitute.For<IMercadoPagoService>();

    private static WebhookTestContext NewContext()
    {
        var options = new DbContextOptionsBuilder<WebhookTestContext>()
            .UseInMemoryDatabase($"webhook-tests-{Guid.NewGuid()}")
            .Options;
        return new WebhookTestContext(options);
    }

    private ProcessWebhookCommandHandler CreateHandler(DbContext ctx, IUnitOfWork? unitOfWork = null)
        => new(
            ctx,
            _mercadoPago,
            unitOfWork ?? new PassthroughUnitOfWork(ctx),
            NullLogger<ProcessWebhookCommandHandler>.Instance);

    private void ArrangeValidPayment(Guid orderId, string status)
    {
        _mercadoPago.ValidateWebhookSignature(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _mercadoPago.GetPaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<MercadoPagoPaymentInfo>.Success(
                new MercadoPagoPaymentInfo("PAY-123", status, 100m, orderId.ToString())));
    }

    private static ProcessWebhookCommand PaymentCommand(string dataId = "PAY-123")
        => new("sig", "req", dataId, "payment");

    private static async Task<(Order order, ProductVariant variant)> SeedOrderAsync(
        WebhookTestContext ctx, OrderStatus status, int stock = 10, int quantity = 2)
    {
        var variant = new ProductVariant
        {
            Sku = "SKU-1",
            Nombre = "500 ml",
            PrecioVenta = 100m,
            PrecioCosto = 50m,
            SizeValue = 500,
            SizeUnit = "ml",
            Stock = stock
        };

        var order = new Order
        {
            UserId = Guid.NewGuid(),
            ShippingAddressId = Guid.NewGuid(),
            Status = status,
            Subtotal = 200m,
            Total = 200m,
            Items =
            [
                new OrderItem
                {
                    VariantId = variant.Id,
                    Quantity = quantity,
                    UnitPrice = 100m,
                    Subtotal = 200m,
                    ProductName = "Ceramic Coating",
                    VariantName = "500 ml",
                    Sku = "SKU-1"
                }
            ]
        };

        ctx.ProductVariants.Add(variant);
        ctx.Orders.Add(order);
        await ctx.SaveChangesAsync();
        return (order, variant);
    }

    [Fact]
    public async Task Handle_PagoAprobado_DescuentaStockUnaVezYMarcaPagada()
    {
        await using var ctx = NewContext();
        var (order, variant) = await SeedOrderAsync(ctx, OrderStatus.Pendiente, stock: 10, quantity: 2);
        ArrangeValidPayment(order.Id, "approved");

        var result = await CreateHandler(ctx).Handle(PaymentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await ctx.ProductVariants.FindAsync(variant.Id);
        updated!.Stock.Should().Be(8);
        (await ctx.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Pagada);
    }

    [Fact]
    public async Task Handle_PagoAprobado_LimpiaCarritoDelUsuario()
    {
        await using var ctx = NewContext();
        var (order, _) = await SeedOrderAsync(ctx, OrderStatus.Pendiente);

        var cart = new Cart { UserId = order.UserId, Items = [new CartItem { VariantId = Guid.NewGuid(), Quantity = 1 }] };
        ctx.Carts.Add(cart);
        await ctx.SaveChangesAsync();

        ArrangeValidPayment(order.Id, "approved");

        await CreateHandler(ctx).Handle(PaymentCommand(), CancellationToken.None);

        (await ctx.CartItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_OrdenYaPagada_NoDescuentaStockDeNuevo()
    {
        await using var ctx = NewContext();
        var (order, variant) = await SeedOrderAsync(ctx, OrderStatus.Pagada, stock: 8, quantity: 2);
        ArrangeValidPayment(order.Id, "approved");

        var result = await CreateHandler(ctx).Handle(PaymentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Guard de estado terminal: el stock NO se vuelve a descontar.
        (await ctx.ProductVariants.FindAsync(variant.Id))!.Stock.Should().Be(8);
    }

    [Fact]
    public async Task Handle_Reembolso_RestauraStockYMarcaReembolsada()
    {
        await using var ctx = NewContext();
        var (order, variant) = await SeedOrderAsync(ctx, OrderStatus.Pendiente, stock: 5, quantity: 3);
        ArrangeValidPayment(order.Id, "refunded");

        var result = await CreateHandler(ctx).Handle(PaymentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await ctx.ProductVariants.FindAsync(variant.Id))!.Stock.Should().Be(8);
        (await ctx.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Reembolsada);
    }

    [Fact]
    public async Task Handle_PagoRechazado_MarcaRechazadaSinTocarStock()
    {
        await using var ctx = NewContext();
        var (order, variant) = await SeedOrderAsync(ctx, OrderStatus.Pendiente, stock: 10, quantity: 2);
        ArrangeValidPayment(order.Id, "rejected");

        var result = await CreateHandler(ctx).Handle(PaymentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await ctx.ProductVariants.FindAsync(variant.Id))!.Stock.Should().Be(10);
        (await ctx.Orders.FindAsync(order.Id))!.Status.Should().Be(OrderStatus.Rechazada);
    }

    [Fact]
    public async Task Handle_FirmaInvalida_Devuelve401SinProcesar()
    {
        await using var ctx = NewContext();
        var (order, variant) = await SeedOrderAsync(ctx, OrderStatus.Pendiente, stock: 10, quantity: 2);
        _mercadoPago.ValidateWebhookSignature(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await CreateHandler(ctx).Handle(PaymentCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(401);
        (await ctx.ProductVariants.FindAsync(variant.Id))!.Stock.Should().Be(10);
        await _mercadoPago.DidNotReceive().GetPaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TipoNoEsPayment_DevuelveExitoSinProcesar()
    {
        await using var ctx = NewContext();
        var command = new ProcessWebhookCommand("sig", "req", "PAY-123", "merchant_order");

        var result = await CreateHandler(ctx).Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _mercadoPago.DidNotReceive().ValidateWebhookSignature(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WebhookConcurrente_DevuelveExitoIdempotente()
    {
        await using var ctx = NewContext();
        var (order, _) = await SeedOrderAsync(ctx, OrderStatus.Pendiente, stock: 10, quantity: 2);
        ArrangeValidPayment(order.Id, "approved");

        // Simula que otro webhook concurrente ya transicionó la orden: el xmin
        // provoca DbUpdateConcurrencyException al persistir.
        var throwingUow = Substitute.For<IUnitOfWork>();
        throwingUow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new DbUpdateConcurrencyException());

        var result = await CreateHandler(ctx, throwingUow).Handle(PaymentCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// IUnitOfWork que delega el guardado al DbContext InMemory de prueba,
    /// replicando el comportamiento real (AppDbContext implementa IUnitOfWork).
    /// </summary>
    private sealed class PassthroughUnitOfWork(DbContext ctx) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => ctx.SaveChangesAsync(ct);
    }
}
