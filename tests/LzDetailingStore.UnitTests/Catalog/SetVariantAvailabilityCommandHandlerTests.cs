using FluentAssertions;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.SetVariantAvailability;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.UnitTests.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Catalog;

public sealed class SetVariantAvailabilityCommandHandlerTests
{
    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;
    private readonly SetVariantAvailabilityCommandHandler _handler;

    public SetVariantAvailabilityCommandHandlerTests()
    {
        _seed = new CatalogSeeder(_ctx);
        _handler = new SetVariantAvailabilityCommandHandler(_ctx, new PassthroughUnitOfWork(_ctx));
    }

    [Fact]
    public async Task Disponible_EstableceStockDisponible()
    {
        var product = await _seed.ProductAsync("Shampoo", stock: 0);
        var variant = product.Variants.Single();

        var result = await _handler.Handle(
            new SetVariantAvailabilityCommand(product.Id, variant.Id, Disponible: true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await StockOfAsync(variant.Id)).Should().Be(ProductVariant.AvailableStock).And.Be(99);
    }

    [Fact]
    public async Task Agotado_EstableceStockEnCero()
    {
        var product = await _seed.ProductAsync("Cera", stock: 99);
        var variant = product.Variants.Single();

        var result = await _handler.Handle(
            new SetVariantAvailabilityCommand(product.Id, variant.Id, Disponible: false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await StockOfAsync(variant.Id)).Should().Be(ProductVariant.UnavailableStock).And.Be(0);
    }

    [Fact]
    public async Task VarianteDeOtroProducto_DevuelveNotFoundSinCambios()
    {
        var product = await _seed.ProductAsync("Shampoo", stock: 99);
        var other = await _seed.ProductAsync("Otro", stock: 99);
        var variant = product.Variants.Single();

        var result = await _handler.Handle(
            new SetVariantAvailabilityCommand(other.Id, variant.Id, Disponible: false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
        (await StockOfAsync(variant.Id)).Should().Be(99);
    }

    [Fact]
    public async Task VarianteInexistente_DevuelveNotFound()
    {
        var product = await _seed.ProductAsync("Shampoo");

        var result = await _handler.Handle(
            new SetVariantAvailabilityCommand(product.Id, Guid.NewGuid(), Disponible: true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    private async Task<int> StockOfAsync(Guid variantId) =>
        await _ctx.Set<ProductVariant>().AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => v.Stock)
            .SingleAsync();
}
