using FluentAssertions;
using LzDetailingStore.Application.Features.Commerce.Commands.CreateCheckout;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.UnitTests.Recommendations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LzDetailingStore.UnitTests.Commerce;

public sealed class CreateCheckoutCommandHandlerTests
{
    private readonly CheckoutTestContext _ctx = CheckoutTestContext.Create();
    private readonly IMercadoPagoService _mercadoPago = Substitute.For<IMercadoPagoService>();
    private readonly Guid _userId = Guid.NewGuid();

    public CreateCheckoutCommandHandlerTests()
    {
        _mercadoPago.CreatePreferenceAsync(
                Arg.Any<string>(), Arg.Any<IList<MercadoPagoItem>>(), Arg.Any<MercadoPagoBackUrls>(),
                Arg.Any<CancellationToken>())
            .Returns(Result<MercadoPagoPreferenceResult>.Success(
                new MercadoPagoPreferenceResult("PREF-1", "https://mp.test/init")));
    }

    [Fact]
    public async Task Checkout_GuardaIpYUserAgentEnElPedido()
    {
        await SeedCartAsync();

        var order = await CheckoutAsync(new CreateCheckoutCommand(_userId, "201.141.10.20", "Mozilla/5.0 (Windows NT 10.0)"));

        order.ClientIp.Should().Be("201.141.10.20");
        order.ClientUserAgent.Should().Be("Mozilla/5.0 (Windows NT 10.0)");
    }

    [Fact]
    public async Task Checkout_SinIpNiUserAgent_CreaElPedidoConNulos()
    {
        await SeedCartAsync();

        var order = await CheckoutAsync(new CreateCheckoutCommand(_userId));

        order.ClientIp.Should().BeNull();
        order.ClientUserAgent.Should().BeNull();
    }

    [Fact]
    public async Task Checkout_IpInvalidaYUserAgentVacio_NoFallaYGuardaNulos()
    {
        await SeedCartAsync();

        var order = await CheckoutAsync(new CreateCheckoutCommand(_userId, "no-es-una-ip", "   "));

        order.ClientIp.Should().BeNull();
        order.ClientUserAgent.Should().BeNull();
    }

    [Fact]
    public async Task Checkout_IpV4MapeadaEnIpV6_SeGuardaComoIpV4()
    {
        await SeedCartAsync();

        var order = await CheckoutAsync(new CreateCheckoutCommand(_userId, "::ffff:189.203.5.7", "UA"));

        order.ClientIp.Should().Be("189.203.5.7");
    }

    [Fact]
    public async Task Checkout_UserAgentDemasiadoLargo_SeTrunca()
    {
        await SeedCartAsync();

        var order = await CheckoutAsync(new CreateCheckoutCommand(_userId, "10.0.0.1", new string('a', 2000)));

        order.ClientUserAgent.Should().HaveLength(Order.ClientUserAgentMaxLength);
    }

    private async Task<Order> CheckoutAsync(CreateCheckoutCommand command)
    {
        var handler = new CreateCheckoutCommandHandler(
            _ctx, _mercadoPago, new PassthroughUnitOfWork(_ctx), Options.Create(new AppSettings()));

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _ctx.ChangeTracker.Clear();
        return await _ctx.Set<Order>().SingleAsync(o => o.Id == result.Value!.OrderId);
    }

    private async Task SeedCartAsync()
    {
        var product = new Product { Name = "Shampoo", Sku = "P-1", Slug = "shampoo", ImageUrl = "img.png" };
        var variant = new ProductVariant
        {
            ProductId = product.Id, Product = product, Sku = "SKU-1", Nombre = "500 ml",
            PrecioVenta = 100m, SizeValue = 500, SizeUnit = "ml", Stock = ProductVariant.AvailableStock
        };
        var cart = new Cart { UserId = _userId };
        cart.Items.Add(new CartItem { CartId = cart.Id, VariantId = variant.Id, Variant = variant, Quantity = 2 });
        var address = new ShippingAddress
        {
            UserId = _userId, Nombre = "Casa", Calle = "Av. Reforma", NumeroExterior = "10", Colonia = "Centro",
            Ciudad = "CDMX", Estado = "CDMX", CodigoPostal = "06000", EsPrincipal = true
        };

        _ctx.AddRange(product, variant, cart, address);
        await _ctx.SaveChangesAsync();
        _ctx.ChangeTracker.Clear();
    }
}

/// <summary>
/// Minimal in-memory DbContext with the entities the checkout handler touches, avoiding the
/// PostgreSQL-specific configuration of the real AppDbContext.
/// </summary>
public sealed class CheckoutTestContext(DbContextOptions<CheckoutTestContext> options) : DbContext(options)
{
    public static CheckoutTestContext Create()
    {
        var options = new DbContextOptionsBuilder<CheckoutTestContext>()
            .UseInMemoryDatabase($"checkout-tests-{Guid.NewGuid()}")
            .Options;
        return new CheckoutTestContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(b =>
        {
            b.HasKey(p => p.Id);
            b.Ignore(p => p.Brand);
            b.Ignore(p => p.Category);
            b.Ignore(p => p.Images);
            b.Ignore(p => p.Specs);
            b.Ignore(p => p.ApplicationSteps);
            b.HasMany(p => p.Variants).WithOne(v => v.Product).HasForeignKey(v => v.ProductId);
        });
        modelBuilder.Entity<ProductVariant>(b => b.HasKey(v => v.Id));
        modelBuilder.Entity<Cart>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasMany(c => c.Items).WithOne(i => i.Cart).HasForeignKey(i => i.CartId);
        });
        modelBuilder.Entity<CartItem>(b =>
        {
            b.HasKey(i => i.Id);
            b.HasOne(i => i.Variant).WithMany().HasForeignKey(i => i.VariantId);
        });
        modelBuilder.Entity<ShippingAddress>(b => b.HasKey(a => a.Id));
        modelBuilder.Entity<Order>(b =>
        {
            b.HasKey(o => o.Id);
            b.HasOne(o => o.ShippingAddress).WithMany().HasForeignKey(o => o.ShippingAddressId);
            b.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId);
        });
        modelBuilder.Entity<OrderItem>(b => b.HasKey(i => i.Id));
    }
}
