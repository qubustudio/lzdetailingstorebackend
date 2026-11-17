using FluentAssertions;
using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProductById;
using LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProducts;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.UnitTests.Recommendations;

namespace LzDetailingStore.UnitTests.Catalog;

public sealed class AdminProductQueriesTests
{
    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;

    public AdminProductQueriesTests()
    {
        _seed = new CatalogSeeder(_ctx);
    }

    // ---- List ----

    [Fact]
    public async Task Lista_IncluyeProductosInactivos()
    {
        await _seed.ProductAsync("Shampoo");
        await _seed.ProductAsync("Cera", active: false);

        var page = await List(new GetAdminProductsQuery());

        page.Total.Should().Be(2);
        page.Data.Select(p => p.Name).Should().Equal("Cera", "Shampoo");
        page.Data.Single(p => p.Name == "Cera").IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Lista_FiltraPorActivo()
    {
        await _seed.ProductAsync("Shampoo");
        await _seed.ProductAsync("Cera", active: false);

        var activos = await List(new GetAdminProductsQuery(Activo: true));
        var inactivos = await List(new GetAdminProductsQuery(Activo: false));

        activos.Data.Should().ContainSingle().Which.Name.Should().Be("Shampoo");
        inactivos.Data.Should().ContainSingle().Which.Name.Should().Be("Cera");
    }

    [Fact]
    public async Task Lista_BuscaPorNombreSinDistinguirMayusculas()
    {
        await _seed.ProductAsync("Shampoo Neutro");
        await _seed.ProductAsync("Cera");

        var page = await List(new GetAdminProductsQuery(Search: "  sHAMpoo "));

        page.Data.Should().ContainSingle().Which.Name.Should().Be("Shampoo Neutro");
    }

    [Fact]
    public async Task Lista_BuscaPorSkuDeVariante()
    {
        await _seed.ProductAsync("Shampoo");
        var cera = await _seed.ProductAsync("Cera");
        cera.Variants.Single().Sku = "CERA-1L-XYZ";
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAdminProductsQuery(Search: "1l-xyz"));

        page.Data.Should().ContainSingle().Which.Id.Should().Be(cera.Id);
    }

    [Fact]
    public async Task Lista_BuscaPorSkuDeProducto()
    {
        var shampoo = await _seed.ProductAsync("Shampoo");
        await _seed.ProductAsync("Cera");

        var page = await List(new GetAdminProductsQuery(Search: shampoo.Sku.ToLowerInvariant()));

        page.Data.Should().ContainSingle().Which.Id.Should().Be(shampoo.Id);
    }

    [Fact]
    public async Task Lista_FiltraPorMarca()
    {
        await _seed.ProductAsync("Shampoo");
        var cera = await _seed.ProductAsync("Cera");
        var otraMarca = new Brand { Nombre = "Marca Dos", Slug = "marca-dos" };
        _ctx.Add(otraMarca);
        cera.Brand = otraMarca;
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAdminProductsQuery(BrandId: otraMarca.Id));

        var item = page.Data.Should().ContainSingle().Subject;
        item.Id.Should().Be(cera.Id);
        item.BrandName.Should().Be("Marca Dos");
    }

    [Fact]
    public async Task Lista_Pagina_OrdenadaPorNombre()
    {
        foreach (var name in new[] { "Delta", "Alfa", "Echo", "Charlie", "Bravo" })
            await _seed.ProductAsync(name);

        var page = await List(new GetAdminProductsQuery(Page: 2, Limit: 2));

        page.Total.Should().Be(5);
        page.TotalPages.Should().Be(3);
        page.Page.Should().Be(2);
        page.Data.Select(p => p.Name).Should().Equal("Charlie", "Delta");
    }

    [Fact]
    public async Task Lista_CuentaVariantesActivasYAgotadas()
    {
        var product = await _seed.ProductAsync("Shampoo", stock: 99);
        AddVariant(product, Variant("V-AGOTADA", stock: 0));
        AddVariant(product, Variant("V-INACTIVA-AGOTADA", stock: 0, active: false));
        AddVariant(product, Variant("V-DISPONIBLE", stock: 99));
        await _ctx.SaveChangesAsync();

        var item = (await List(new GetAdminProductsQuery())).Data.Single();

        item.TotalVariantes.Should().Be(3);
        item.VariantesAgotadas.Should().Be(1);
    }

    [Fact]
    public async Task Lista_AgotadosAlguno_SoloProductosConVarianteActivaAgotada()
    {
        var conAgotada = await _seed.ProductAsync("Shampoo", stock: 99);
        AddVariant(conAgotada, Variant("V-AGOTADA", stock: 0));
        var soloInactivaAgotada = await _seed.ProductAsync("Cera", stock: 99);
        AddVariant(soloInactivaAgotada, Variant("V-INACTIVA-AGOTADA", stock: 0, active: false));
        await _seed.ProductAsync("Abrillantador", stock: 99);
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAdminProductsQuery(Agotados: "alguno"));

        page.Total.Should().Be(1);
        page.Data.Should().ContainSingle().Which.Id.Should().Be(conAgotada.Id);
    }

    [Fact]
    public async Task Lista_AgotadosCompleto_SoloProductosSinVarianteActivaDisponible()
    {
        var todoAgotado = await _seed.ProductAsync("Shampoo", stock: 0);
        var parcial = await _seed.ProductAsync("Cera", stock: 99);
        AddVariant(parcial, Variant("V-AGOTADA", stock: 0));
        var soloInactivaDisponible = await _seed.ProductAsync("Abrillantador", stock: 0);
        AddVariant(soloInactivaDisponible, Variant("V-INACTIVA-DISPONIBLE", stock: 99, active: false));
        await _seed.ProductAsync("Desengrasante", stock: 99);
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAdminProductsQuery(Agotados: " COMPLETO "));

        page.Data.Select(p => p.Id).Should().BeEquivalentTo(new[] { todoAgotado.Id, soloInactivaDisponible.Id });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Lista_AgotadosVacioONulo_NoFiltra(string? agotados)
    {
        var conAgotada = await _seed.ProductAsync("Shampoo", stock: 99);
        AddVariant(conAgotada, Variant("V-AGOTADA", stock: 0));
        await _seed.ProductAsync("Cera", stock: 99);
        await _ctx.SaveChangesAsync();

        var page = await List(new GetAdminProductsQuery(Agotados: agotados));

        page.Total.Should().Be(2);
    }

    [Fact]
    public async Task Lista_ReportaIncluyeCostos()
    {
        await _seed.ProductAsync("Shampoo");

        (await List(new GetAdminProductsQuery(IncluyeCostos: true))).IncluyeCostos.Should().BeTrue();
        (await List(new GetAdminProductsQuery())).IncluyeCostos.Should().BeFalse();
    }

    // ---- Detail ----

    [Fact]
    public async Task Detalle_SinPermisoCostos_CostoNulo()
    {
        var product = await _seed.ProductAsync("Shampoo");

        var detail = await Detail(new GetAdminProductByIdQuery(product.Id));

        detail.IncluyeCostos.Should().BeFalse();
        detail.Variants.Should().ContainSingle().Which.PrecioCosto.Should().BeNull();
    }

    [Fact]
    public async Task Detalle_ConPermisoCostos_IncluyeCosto()
    {
        var product = await _seed.ProductAsync("Shampoo");

        var detail = await Detail(new GetAdminProductByIdQuery(product.Id, IncluyeCostos: true));

        detail.IncluyeCostos.Should().BeTrue();
        detail.Variants.Should().ContainSingle().Which.PrecioCosto.Should().Be(87.35m);
    }

    [Fact]
    public async Task Detalle_IncluyeProductoYVariantesInactivas_OrdenadasYConDisponible()
    {
        var product = await _seed.ProductAsync("Shampoo", active: false, stock: 0);
        product.Variants.Single().SortOrder = 2;
        var inactive = Variant("V-INACTIVA", stock: 99, active: false);
        inactive.SortOrder = 1;
        AddVariant(product, inactive);
        await _ctx.SaveChangesAsync();

        var detail = await Detail(new GetAdminProductByIdQuery(product.Id));

        detail.IsActive.Should().BeFalse();
        detail.BrandName.Should().Be("Marca Uno");
        detail.CategoryName.Should().Be("Lavado");
        detail.Variants.Select(v => v.Sku).Should().Equal("V-INACTIVA", product.Variants.First(v => v.SortOrder == 2).Sku);
        detail.Variants[0].Should().BeEquivalentTo(new { IsActive = false, Disponible = true });
        detail.Variants[1].Should().BeEquivalentTo(new { IsActive = true, Disponible = false });
    }

    [Fact]
    public async Task Detalle_Inexistente_DevuelveNotFound()
    {
        var handler = new GetAdminProductByIdQueryHandler(_ctx);

        var result = await handler.Handle(new GetAdminProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }

    // ---- Stock is never exposed ----

    [Theory]
    [InlineData(typeof(AdminProductPageDto))]
    [InlineData(typeof(AdminProductListItemDto))]
    [InlineData(typeof(AdminProductDetailDto))]
    [InlineData(typeof(AdminProductVariantDto))]
    public void Dtos_NoExponenStock(Type dto)
    {
        dto.GetProperties().Select(p => p.Name)
            .Should().NotContain(name => name.Contains("Stock", StringComparison.OrdinalIgnoreCase));
    }

    // Added through the context: a client-generated Id on a navigation add would be tracked as Modified.
    private void AddVariant(Product product, ProductVariant variant)
    {
        variant.ProductId = product.Id;
        _ctx.Add(variant);
    }

    private static ProductVariant Variant(string sku, int stock, bool active = true) => new()
    {
        Sku = sku,
        Nombre = sku,
        PrecioVenta = 150m,
        PrecioCosto = 60m,
        SizeValue = 1,
        SizeUnit = "L",
        Stock = stock,
        IsActive = active
    };

    private async Task<AdminProductPageDto> List(GetAdminProductsQuery query)
    {
        var result = await new GetAdminProductsQueryHandler(_ctx).Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    private async Task<AdminProductDetailDto> Detail(GetAdminProductByIdQuery query)
    {
        var result = await new GetAdminProductByIdQueryHandler(_ctx).Handle(query, CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }
}
