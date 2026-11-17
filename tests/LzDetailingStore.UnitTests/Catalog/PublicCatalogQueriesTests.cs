using FluentAssertions;
using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Application.Features.Catalog.Queries.GetProductBySlug;
using LzDetailingStore.Application.Features.Catalog.Queries.GetProducts;
using LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.UnitTests.Recommendations;

namespace LzDetailingStore.UnitTests.Catalog;

/// <summary>
/// Public catalog rules: products without visible presentations are hidden, fully sold-out products go last
/// and the "desde" price is computed from available presentations (falling back to visible ones).
/// </summary>
public sealed class PublicCatalogQueriesTests
{
    private const int Disponible = 99;
    private const int Agotado = 0;

    private static readonly DateTime Base = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;

    public PublicCatalogQueriesTests() => _seed = new CatalogSeeder(_ctx);

    // ---- Products without visible presentations ----

    [Fact]
    public async Task Lista_OcultaProductosSinPresentacionesVisibles()
    {
        await _seed.ProductWithVariantsAsync("Con presentación", null, (199m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Solo inactivas", null, (199m, Disponible, false));
        await _seed.ProductWithVariantsAsync("Sin variantes", null);
        await _seed.ProductAsync("Producto inactivo", active: false);

        var page = await List(new GetProductsQuery());

        page.Total.Should().Be(1);
        page.Data.Select(p => p.Name).Should().Equal("Con presentación");
    }

    [Fact]
    public async Task Detalle_ProductoSinPresentacionesVisibles_Devuelve404()
    {
        var vacio = await _seed.ProductWithVariantsAsync("Solo inactivas", null, (199m, Disponible, false));

        var result = await Detail(vacio.Slug);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Detalle_ProductoAgotado_SeMuestraConSusPresentaciones()
    {
        var agotado = await _seed.ProductWithVariantsAsync("Agotado", null,
            (250m, Agotado, true), (180m, Agotado, true), (90m, Disponible, false));

        var result = await Detail(agotado.Slug);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Variants.Should().HaveCount(2).And.OnlyContain(v => !v.Disponible);
        result.Value.PrecioBase.Should().Be(180m);
    }

    [Fact]
    public async Task Recomendaciones_ProductoSinPresentacionesVisibles_Devuelve404()
    {
        var vacio = await _seed.ProductWithVariantsAsync("Solo inactivas", null, (199m, Disponible, false));

        var result = await new GetRecommendationsQueryHandler(_ctx)
            .Handle(new GetRecommendationsQuery(vacio.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Recomendaciones_ExcluyenRelacionadosSinPresentacionesVisibles()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var visible = await _seed.ProductWithVariantsAsync("Visible", null, (199m, Disponible, true));
        var vacio = await _seed.ProductWithVariantsAsync("Solo inactivas", null, (199m, Disponible, false));
        var sinVariantes = await _seed.ProductWithVariantsAsync("Sin variantes", null);

        await _seed.RelationAsync(p, visible, ProductRelationType.Complementary, sortOrder: 0);
        await _seed.RelationAsync(p, vacio, ProductRelationType.Complementary, sortOrder: 1);
        await _seed.RelationAsync(p, sinVariantes, ProductRelationType.Complementary, sortOrder: 2);

        var result = await new GetRecommendationsQueryHandler(_ctx)
            .Handle(new GetRecommendationsQuery(p.Id), CancellationToken.None);

        result.Value!.Complementarios.Select(x => x.Id).Should().Equal(visible.Id);
    }

    [Fact]
    public async Task Recomendaciones_PrecioDesdeEsLaPresentacionDisponibleMasBarata()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var parcial = await _seed.ProductWithVariantsAsync("Parcial", null,
            (99m, Agotado, true), (150m, Disponible, true), (300m, Disponible, true));
        await _seed.RelationAsync(p, parcial, ProductRelationType.Similar);

        var result = await new GetRecommendationsQueryHandler(_ctx)
            .Handle(new GetRecommendationsQuery(p.Id), CancellationToken.None);

        result.Value!.Similares.Should().ContainSingle().Which.PrecioBase.Should().Be(150m);
    }

    // ---- Sold-out last ----

    [Fact]
    public async Task Lista_AgotadosAlFinal_RespetandoOrdenPorDefecto()
    {
        await _seed.ProductWithVariantsAsync("Agotado nuevo", Base.AddDays(5), (100m, Agotado, true));
        await _seed.ProductWithVariantsAsync("Disponible viejo", Base.AddDays(1), (100m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Agotado viejo", Base.AddDays(2), (100m, Agotado, true));
        await _seed.ProductWithVariantsAsync("Disponible nuevo", Base.AddDays(4),
            (100m, Agotado, true), (120m, Disponible, true));

        var page = await List(new GetProductsQuery());

        page.Data.Select(p => p.Name).Should().Equal(
            "Disponible nuevo", "Disponible viejo", "Agotado nuevo", "Agotado viejo");
    }

    [Fact]
    public async Task Lista_AgotadosAlFinal_RespetandoOrdenPorNombre()
    {
        await _seed.ProductWithVariantsAsync("A agotado", null, (100m, Agotado, true));
        await _seed.ProductWithVariantsAsync("C disponible", null, (100m, Disponible, true));
        await _seed.ProductWithVariantsAsync("B disponible", null, (100m, Disponible, true));

        var page = await List(new GetProductsQuery(SortBy: "name"));

        page.Data.Select(p => p.Name).Should().Equal("B disponible", "C disponible", "A agotado");
    }

    [Fact]
    public async Task Lista_PaginacionRespetaAgotadosAlFinal()
    {
        await _seed.ProductWithVariantsAsync("Agotado", Base.AddDays(9), (100m, Agotado, true));
        await _seed.ProductWithVariantsAsync("Disponible 1", Base.AddDays(2), (100m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Disponible 2", Base.AddDays(1), (100m, Disponible, true));

        var first = await List(new GetProductsQuery(Page: 1, Limit: 2));
        var second = await List(new GetProductsQuery(Page: 2, Limit: 2));

        first.Total.Should().Be(3);
        first.Data.Select(p => p.Name).Should().Equal("Disponible 1", "Disponible 2");
        second.Data.Select(p => p.Name).Should().Equal("Agotado");
    }

    // ---- Computed "desde" price ----

    [Fact]
    public async Task PrecioDesde_ParcialmenteAgotado_UsaLaDisponibleMasBarata()
    {
        var parcial = await _seed.ProductWithVariantsAsync("Parcial", null,
            (99m, Agotado, true), (150m, Disponible, true), (300m, Disponible, true), (50m, Disponible, false));

        var page = await List(new GetProductsQuery());
        var detail = await Detail(parcial.Slug);

        page.Data.Single().PrecioBase.Should().Be(150m);
        detail.Value!.PrecioBase.Should().Be(150m);
    }

    [Fact]
    public async Task PrecioDesde_TotalmenteAgotado_UsaLaVisibleMasBarata()
    {
        await _seed.ProductWithVariantsAsync("Agotado", null,
            (250m, Agotado, true), (180m, Agotado, true), (90m, Disponible, false));

        var page = await List(new GetProductsQuery());

        page.Data.Single().PrecioBase.Should().Be(180m);
    }

    [Fact]
    public async Task PrecioDesde_NoModificaElPrecioGuardado()
    {
        var parcial = await _seed.ProductWithVariantsAsync("Parcial", null,
            (99m, Agotado, true), (150m, Disponible, true));

        await List(new GetProductsQuery());

        _ctx.ChangeTracker.Clear();
        (await _ctx.Set<Domain.Entities.Catalog.Product>().FindAsync(parcial.Id))!.PrecioBase.Should().Be(1m);
    }

    [Fact]
    public async Task OrdenPorPrecio_UsaPrecioDesdeCalculado()
    {
        // Cheapest visible variant is sold out: by computed price "Parcial" costs 150, not 10.
        await _seed.ProductWithVariantsAsync("Parcial", null, (10m, Agotado, true), (150m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Medio", null, (100m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Caro", null, (200m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Agotado barato", null, (5m, Agotado, true));

        var asc = await List(new GetProductsQuery(SortBy: "price_asc"));
        var desc = await List(new GetProductsQuery(SortBy: "price_desc"));

        asc.Data.Select(p => p.Name).Should().Equal("Medio", "Parcial", "Caro", "Agotado barato");
        desc.Data.Select(p => p.Name).Should().Equal("Caro", "Parcial", "Medio", "Agotado barato");
    }

    [Fact]
    public async Task FiltroDePrecio_UsaPrecioDesdeCalculado()
    {
        await _seed.ProductWithVariantsAsync("Parcial", null, (10m, Agotado, true), (150m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Barato", null, (50m, Disponible, true));
        await _seed.ProductWithVariantsAsync("Agotado", null, (300m, Agotado, true), (120m, Agotado, true));

        var desde100 = await List(new GetProductsQuery(MinPrice: 100m));
        var hasta100 = await List(new GetProductsQuery(MaxPrice: 100m));

        desde100.Data.Select(p => p.Name).Should().BeEquivalentTo("Parcial", "Agotado");
        hasta100.Data.Select(p => p.Name).Should().Equal("Barato");
    }

    /// <summary>Runs the detail query on a clean tracker, as a real request would (seeded entities keep all variants loaded).</summary>
    private Task<Result<ProductDto>> Detail(string slug)
    {
        _ctx.ChangeTracker.Clear();
        return new GetProductBySlugQueryHandler(_ctx).Handle(new GetProductBySlugQuery(slug), CancellationToken.None);
    }

    private async Task<PagedResult<ProductListDto>> List(GetProductsQuery query)
    {
        var result = await new GetProductsQueryHandler(_ctx).Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }
}
