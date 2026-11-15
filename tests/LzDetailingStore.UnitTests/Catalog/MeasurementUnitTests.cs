using FluentAssertions;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;
using LzDetailingStore.Application.Features.Catalog.Queries.GetMeasurementUnits;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.UnitTests.Recommendations;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.UnitTests.Catalog;

public sealed class MeasurementUnitTests
{
    private const string UnknownUnitMessage = "La unidad de medida no existe.";

    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;
    private readonly PassthroughUnitOfWork _unitOfWork;

    public MeasurementUnitTests()
    {
        _seed = new CatalogSeeder(_ctx);
        _seed.MeasurementUnits();
        _unitOfWork = new PassthroughUnitOfWork(_ctx);
    }

    // ---- GetMeasurementUnits ----

    [Fact]
    public async Task Unidades_DevuelveCatalogoOrdenadoPorOrden()
    {
        var result = await new GetMeasurementUnitsQueryHandler(_ctx)
            .Handle(new GetMeasurementUnitsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(u => u.Clave).Should().Equal("ml", "L", "g", "kg", "pza", "kit");
        result.Value!.First().Nombre.Should().Be("Mililitros");
    }

    [Fact]
    public async Task Unidades_SinVariantes_DevuelveTamanosVacios()
    {
        var result = await new GetMeasurementUnitsQueryHandler(_ctx)
            .Handle(new GetMeasurementUnitsQuery(), CancellationToken.None);

        result.Value!.Should().OnlyContain(u => u.Tamanos.Count == 0);
    }

    [Fact]
    public async Task Unidades_DevuelveTamanosDistintosOrdenadosPorUnidad()
    {
        var product = await _seed.ProductAsync("Shampoo"); // seeds a 500 ml variant
        AddVariants(product,
            (1000m, "ml", true),
            (250m, "ml", true),
            (500m, "ml", true),
            (3.79m, "L", true),
            (1m, "L", true),
            (3.79m, "L", true));
        await _ctx.SaveChangesAsync();

        var result = await new GetMeasurementUnitsQueryHandler(_ctx)
            .Handle(new GetMeasurementUnitsQuery(), CancellationToken.None);

        var byUnit = result.Value!.ToDictionary(u => u.Clave, u => u.Tamanos);
        byUnit["ml"].Should().Equal(250m, 500m, 1000m);
        byUnit["L"].Should().Equal(1m, 3.79m);
        byUnit["kg"].Should().BeEmpty();
        byUnit["kit"].Should().BeEmpty();
    }

    [Fact]
    public async Task Unidades_IncluyeTamanosDeVariantesInactivas()
    {
        var product = await _seed.ProductAsync("Shampoo", active: false, variantActive: false);
        AddVariants(product, (5m, "kg", false));
        await _ctx.SaveChangesAsync();

        var result = await new GetMeasurementUnitsQueryHandler(_ctx)
            .Handle(new GetMeasurementUnitsQuery(), CancellationToken.None);

        var byUnit = result.Value!.ToDictionary(u => u.Clave, u => u.Tamanos);
        byUnit["ml"].Should().Equal(500m);
        byUnit["kg"].Should().Equal(5m);
    }

    // ---- CreateVariant ----

    [Theory]
    [InlineData("litros")]
    [InlineData("l")]
    [InlineData("oz")]
    public async Task CrearVariante_UnidadInexistente_DevuelveErrorDeValidacion(string unit)
    {
        var product = await _seed.ProductAsync("Shampoo");

        var result = await CreateVariant(product.Id, unit);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error!.Message.Should().Be(UnknownUnitMessage);
        (await _ctx.Set<ProductVariant>().CountAsync(v => v.ProductId == product.Id)).Should().Be(1);
    }

    [Fact]
    public async Task CrearVariante_UnidadDelCatalogo_SeGuarda()
    {
        var product = await _seed.ProductAsync("Shampoo");

        var result = await CreateVariant(product.Id, "L");

        result.IsSuccess.Should().BeTrue();
        (await SavedVariantAsync(result.Value)).SizeUnit.Should().Be("L");
    }

    // ---- UpdateVariant ----

    [Fact]
    public async Task ActualizarVariante_UnidadInexistente_DevuelveErrorYNoModifica()
    {
        var variant = (await _seed.ProductAsync("Shampoo")).Variants.Single();

        var result = await UpdateVariant(variant, "litros");

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(400);
        result.Error!.Message.Should().Be(UnknownUnitMessage);
        (await SavedVariantAsync(variant.Id)).SizeUnit.Should().Be("ml");
    }

    [Fact]
    public async Task ActualizarVariante_UnidadDelCatalogo_SeGuarda()
    {
        var variant = (await _seed.ProductAsync("Shampoo")).Variants.Single();

        var result = await UpdateVariant(variant, "kg");

        result.IsSuccess.Should().BeTrue();
        (await SavedVariantAsync(variant.Id)).SizeUnit.Should().Be("kg");
    }

    private Task<LzDetailingStore.SharedKernel.Result<Guid>> CreateVariant(Guid productId, string unit) =>
        new CreateVariantCommandHandler(_ctx, _unitOfWork)
            .Handle(new CreateVariantCommand(productId, "NEW-1", "1 litro", 199m, 87.35m, 1, unit, 99, null, 1),
                CancellationToken.None);

    private Task<LzDetailingStore.SharedKernel.Result> UpdateVariant(ProductVariant variant, string unit) =>
        new UpdateVariantCommandHandler(_ctx, _unitOfWork)
            .Handle(new UpdateVariantCommand(variant.Id, variant.Sku, "1 litro", 199m, 87.35m, 1, unit, null, null, true, 0),
                CancellationToken.None);

    private void AddVariants(Product product, params (decimal Size, string Unit, bool Active)[] variants)
    {
        foreach (var (size, unit, active) in variants)
        {
            _ctx.Add(new ProductVariant
            {
                ProductId = product.Id,
                Sku = $"{product.Sku}-{unit}-{size}-{Guid.NewGuid():N}",
                Nombre = $"{size} {unit}",
                PrecioVenta = 199m,
                PrecioCosto = 87.35m,
                SizeValue = size,
                SizeUnit = unit,
                Stock = ProductVariant.AvailableStock,
                IsActive = active
            });
        }
    }

    private Task<ProductVariant> SavedVariantAsync(Guid id) =>
        _ctx.Set<ProductVariant>().AsNoTracking().SingleAsync(v => v.Id == id);
}
