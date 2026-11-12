using System.Text.Json;
using FluentAssertions;
using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Application.Features.Intelligence.Queries.GetRecommendations;
using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.UnitTests.Recommendations;

public sealed class GetRecommendationsQueryHandlerTests
{
    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;

    public GetRecommendationsQueryHandlerTests() => _seed = new CatalogSeeder(_ctx);

    [Fact]
    public async Task DevuelveSoloAprobadasYVisibles_OrdenadasPorSortOrder_ConRazon()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var a = await _seed.ProductAsync("A Visible", stock: 3);
        var b = await _seed.ProductAsync("B Visible", stock: 7);
        var c = await _seed.ProductAsync("C Borrador");
        var d = await _seed.ProductAsync("D Agotado", stock: 0);
        var e = await _seed.ProductAsync("E Inactivo", active: false);
        var f = await _seed.ProductAsync("F Similar");
        var g = await _seed.ProductAsync("G Variante Inactiva", variantActive: false);
        var h = await _seed.ProductAsync("H Rechazado");

        await _seed.RelationAsync(p, a, ProductRelationType.Complementary, sortOrder: 1, reason: "Razón A");
        await _seed.RelationAsync(p, b, ProductRelationType.Complementary, sortOrder: 0, reason: "Razón B");
        await _seed.RelationAsync(p, c, ProductRelationType.Complementary, ProductRelationStatus.Draft);
        await _seed.RelationAsync(p, d, ProductRelationType.Complementary, sortOrder: 2);
        await _seed.RelationAsync(p, g, ProductRelationType.Complementary, sortOrder: 3);
        await _seed.RelationAsync(p, e, ProductRelationType.Similar);
        await _seed.RelationAsync(p, h, ProductRelationType.Similar, ProductRelationStatus.Rejected);
        await _seed.RelationAsync(p, f, ProductRelationType.Similar, sortOrder: 5, reason: "Razón F");

        var result = await new GetRecommendationsQueryHandler(_ctx)
            .Handle(new GetRecommendationsQuery(p.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var value = result.Value!;
        value.Complementarios.Select(x => x.Id).Should().Equal(b.Id, a.Id);
        value.Complementarios.Select(x => x.Reason).Should().Equal("Razón B", "Razón A");
        value.Similares.Select(x => x.Id).Should().Equal(f.Id);
        value.Similares[0].Reason.Should().Be("Razón F");
        value.Complementarios[0].Variants.Should().ContainSingle().Which.Disponible.Should().BeTrue();
    }

    [Fact]
    public async Task Respuesta_NoExponeCantidadesDeStockNiCosto()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var a = await _seed.ProductAsync("A Visible", stock: 4321);
        await _seed.RelationAsync(p, a, ProductRelationType.Complementary, reason: "Razón");

        var result = await new GetRecommendationsQueryHandler(_ctx)
            .Handle(new GetRecommendationsQuery(p.Id), CancellationToken.None);

        typeof(ProductListVariantDto).GetProperties().Select(x => x.Name)
            .Should().NotContain(n => n.Contains("Stock") || n.Contains("Costo"));

        var json = JsonSerializer.Serialize(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        json.Should().Contain("\"complementarios\"").And.Contain("\"similares\"").And.Contain("\"reason\"");
        json.Should().NotContain("4321").And.NotContainEquivalentOf("stock").And.NotContainEquivalentOf("costo");
    }

    [Fact]
    public async Task Count_LimitaResultadosPorTipo()
    {
        var p = await _seed.ProductAsync("Producto Base");
        for (var i = 0; i < 8; i++)
        {
            var related = await _seed.ProductAsync($"Relacionado {i}");
            await _seed.RelationAsync(p, related, ProductRelationType.Complementary, sortOrder: i);
        }

        var handler = new GetRecommendationsQueryHandler(_ctx);

        var two = await handler.Handle(new GetRecommendationsQuery(p.Id, 2), CancellationToken.None);
        var tooMany = await handler.Handle(new GetRecommendationsQuery(p.Id, 50), CancellationToken.None);

        two.Value!.Complementarios.Should().HaveCount(2);
        tooMany.Value!.Complementarios.Should().HaveCount(6);
    }

    [Fact]
    public async Task ProductoInexistente_Devuelve404()
    {
        var result = await new GetRecommendationsQueryHandler(_ctx)
            .Handle(new GetRecommendationsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(404);
    }
}
