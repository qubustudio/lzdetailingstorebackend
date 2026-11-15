using FluentAssertions;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.SetProductActive;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.UnitTests.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Catalog;

public sealed class AdminProductCommandsTests
{
    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;
    private readonly PassthroughUnitOfWork _unitOfWork;

    public AdminProductCommandsTests()
    {
        _seed = new CatalogSeeder(_ctx);
        _seed.MeasurementUnits();
        _unitOfWork = new PassthroughUnitOfWork(_ctx);
    }

    // ---- SetProductActive ----

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task EstadoProducto_CambiaIsActive(bool initial, bool activo)
    {
        var product = await _seed.ProductAsync("Shampoo", active: initial);

        var result = await new SetProductActiveCommandHandler(_ctx, _unitOfWork)
            .Handle(new SetProductActiveCommand(product.Id, activo), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _ctx.Set<Product>().AsNoTracking().SingleAsync(p => p.Id == product.Id))
            .IsActive.Should().Be(activo);
    }

    [Fact]
    public async Task EstadoProducto_Inexistente_DevuelveNotFound()
    {
        var result = await new SetProductActiveCommandHandler(_ctx, _unitOfWork)
            .Handle(new SetProductActiveCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    // ---- UpdateVariant: stock as availability flag ----

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task ActualizarVariante_SinStock_ConservaDisponibilidad(int stock)
    {
        var variant = (await _seed.ProductAsync("Shampoo", stock: stock)).Variants.Single();

        var result = await UpdateVariant(variant, stock: null);

        result.IsSuccess.Should().BeTrue();
        var saved = await SavedVariantAsync(variant.Id);
        saved.Stock.Should().Be(stock);
        saved.Nombre.Should().Be("1 litro");
    }

    [Theory]
    [InlineData(5, ProductVariant.AvailableStock)]
    [InlineData(0, ProductVariant.UnavailableStock)]
    public async Task ActualizarVariante_ConStock_NormalizaDisponibilidad(int stock, int expected)
    {
        var variant = (await _seed.ProductAsync("Shampoo", stock: 99)).Variants.Single();

        var result = await UpdateVariant(variant, stock);

        result.IsSuccess.Should().BeTrue();
        (await SavedVariantAsync(variant.Id)).Stock.Should().Be(expected);
    }

    [Fact]
    public async Task ActualizarVariante_RecalculaPrecioBase()
    {
        var product = await _seed.ProductAsync("Shampoo");

        await UpdateVariant(product.Variants.Single(), stock: null, precioVenta: 249m);

        (await _ctx.Set<Product>().AsNoTracking().SingleAsync(p => p.Id == product.Id))
            .PrecioBase.Should().Be(249m);
    }

    [Fact]
    public void ValidadorVariante_StockOpcional_PeroNoNegativo()
    {
        var validator = new UpdateVariantCommandValidator();

        validator.Validate(Command(Guid.NewGuid(), stock: null)).IsValid.Should().BeTrue();
        validator.Validate(Command(Guid.NewGuid(), stock: -1)).IsValid.Should().BeFalse();
    }

    private Task<LzDetailingStore.SharedKernel.Result> UpdateVariant(
        ProductVariant variant, int? stock, decimal precioVenta = 199m) =>
        new UpdateVariantCommandHandler(_ctx, _unitOfWork)
            .Handle(Command(variant.Id, stock, variant.Sku, precioVenta), CancellationToken.None);

    private static UpdateVariantCommand Command(
        Guid id, int? stock, string sku = "V-1", decimal precioVenta = 199m) =>
        new(id, sku, "1 litro", precioVenta, 87.35m, 1, "L", stock, null, true, 0);

    private Task<ProductVariant> SavedVariantAsync(Guid id) =>
        _ctx.Set<ProductVariant>().AsNoTracking().SingleAsync(v => v.Id == id);
}
