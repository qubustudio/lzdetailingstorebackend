using FluentAssertions;
using LzDetailingStore.Application.Features.Recommendations;
using LzDetailingStore.Application.Features.Recommendations.Commands.GenerateProductRelations;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace LzDetailingStore.UnitTests.Recommendations;

public sealed class GenerateProductRelationsCommandHandlerTests
{
    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;

    public GenerateProductRelationsCommandHandlerTests() => _seed = new CatalogSeeder(_ctx);

    private GenerateProductRelationsCommandHandler CreateHandler(IChatClient? chatClient) =>
        new(_ctx, new PassthroughUnitOfWork(_ctx), NullLogger<GenerateProductRelationsCommandHandler>.Instance, chatClient);

    private static string Entry(int sourceRef, string complementary = "", string similar = "") =>
        $$"""{ "ref": {{sourceRef}}, "complementary": [{{complementary}}], "similar": [{{similar}}] }""";

    private static string Products(params string[] entries) => $$"""{ "products": [{{string.Join(",", entries)}}] }""";

    private static string S(int r, string reason = "Ideal para tu rutina.") => $$"""{ "ref": {{r}}, "reason": "{{reason}}" }""";

    [Fact]
    public async Task Validacion_DescartaDesconocidos_Inactivos_AutoReferencias_Duplicados_YRechazados()
    {
        // Catalog refs follow name order among ACTIVE products: A=1, B=2, C=3, D=4, F=5.
        var a = await _seed.ProductAsync("A Shampoo");
        var b = await _seed.ProductAsync("B Guante");
        var c = await _seed.ProductAsync("C Shampoo Plus");
        var d = await _seed.ProductAsync("D Shampoo Ceramico");
        await _seed.ProductAsync("E Inactivo", active: false);
        var f = await _seed.ProductAsync("F Toalla");
        await _seed.RelationAsync(a, b, ProductRelationType.Complementary, ProductRelationStatus.Rejected);
        await _seed.RelationAsync(a, c, ProductRelationType.Similar, ProductRelationStatus.Draft);

        var json = Products(Entry(1,
            complementary: string.Join(",", S(1), S(2), S(99), S(5, "  Seca sin rayar.  ")),
            similar: string.Join(",", S(3), S(4), S(4))));
        var chat = new FakeChatClient(_ => FakeChatClient.Json(json));

        var result = await CreateHandler(chat).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.Fill), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var created = await _ctx.Set<ProductRelation>()
            .Where(r => r.Source == ProductRelationSource.Ai)
            .ToListAsync();

        created.Should().HaveCount(2);
        created.Should().ContainSingle(r => r.ProductId == a.Id && r.RelatedProductId == f.Id
                                            && r.Type == ProductRelationType.Complementary
                                            && r.Reason == "Seca sin rayar.");
        created.Should().ContainSingle(r => r.ProductId == a.Id && r.RelatedProductId == d.Id
                                            && r.Type == ProductRelationType.Similar
                                            && r.SortOrder == 1);
        created.Should().OnlyContain(r => r.Status == ProductRelationStatus.Draft);
        result.Value!.RelationsCreated.Should().Be(2);
        result.Value.BatchesSucceeded.Should().Be(1);
    }

    [Fact]
    public async Task Prompt_IncluyeSoloProductosActivos_SinStockNiCosto_YUsaJsonSchema()
    {
        await _seed.ProductAsync("A Shampoo", stock: 4321);
        await _seed.ProductAsync("B Guante");
        await _seed.ProductAsync("E Inactivo", active: false);
        var chat = new FakeChatClient(_ => FakeChatClient.Json(Products()));

        await CreateHandler(chat).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.New), CancellationToken.None);

        chat.Calls.Should().NotBeEmpty();
        var call = chat.Calls[0];
        call.SystemPrompt.Should().Contain("A Shampoo").And.Contain("B Guante").And.NotContain("E Inactivo");
        call.SystemPrompt.Should().NotContain("4321").And.NotContain("87.35").And.NotContain("87,35");
        call.Options!.ResponseFormat.Should().BeOfType<ChatResponseFormatJson>()
            .Which.Schema.Should().NotBeNull();
    }

    [Fact]
    public async Task Validacion_LimitaATresPorTipo()
    {
        var a = await _seed.ProductAsync("A Shampoo");
        for (var i = 0; i < 5; i++)
            await _seed.ProductAsync($"B Producto {i}");

        var json = Products(Entry(1, complementary: string.Join(",", S(2), S(3), S(4), S(5), S(6))));
        var chat = new FakeChatClient(call => FakeChatClient.Json(
            call.UserPrompt.Contains("just added") ? Products() : json));

        await CreateHandler(chat).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.New), CancellationToken.None);

        var created = await _ctx.Set<ProductRelation>().Where(r => r.ProductId == a.Id).ToListAsync();
        created.Should().HaveCount(3);
        created.Select(r => r.SortOrder).Should().BeEquivalentTo([0, 1, 2]);
    }

    [Fact]
    public async Task ModoNuevos_CreaRelacionesEnAmbasDirecciones()
    {
        // Refs: N=1, X=2, Y=3. X and Y already have relations, so only N is a source.
        var n = await _seed.ProductAsync("N Nuevo Sellador");
        var x = await _seed.ProductAsync("X Aplicador");
        var y = await _seed.ProductAsync("Y Sellador Clasico");
        await _seed.RelationAsync(x, y, ProductRelationType.Complementary);
        await _seed.RelationAsync(y, x, ProductRelationType.Complementary);

        var chat = new FakeChatClient(call => call.UserPrompt.Contains("just added")
            ? FakeChatClient.Json(Products(Entry(1, similar: S(3, "Una alternativa moderna a tu sellador."))))
            : FakeChatClient.Json(Products(Entry(1, complementary: S(2, "Aplica el sellador de forma uniforme.")))));

        var result = await CreateHandler(chat).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.New), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.SourceProducts.Should().Be(1);
        chat.Calls.Should().HaveCount(2);

        var created = await _ctx.Set<ProductRelation>().Where(r => r.Source == ProductRelationSource.Ai).ToListAsync();
        created.Should().HaveCount(2);
        created.Should().ContainSingle(r => r.ProductId == n.Id && r.RelatedProductId == x.Id
                                            && r.Type == ProductRelationType.Complementary);
        created.Should().ContainSingle(r => r.ProductId == y.Id && r.RelatedProductId == n.Id
                                            && r.Type == ProductRelationType.Similar
                                            && r.Reason == "Una alternativa moderna a tu sellador.");
    }

    [Theory]
    [InlineData("excepcion")]
    [InlineData("rechazo")]
    [InlineData("json_invalido")]
    public async Task LoteFallido_SeOmite_YContinuaConElSiguienteLote(string failure)
    {
        // 12 products without approved relations -> Fill mode creates 2 batches (10 + 2), one call each.
        var products = new List<Product>();
        for (var i = 1; i <= 12; i++)
            products.Add(await _seed.ProductAsync($"Producto {i:00}"));

        var chat = new FakeChatClient(call =>
        {
            if (call.Index == 0)
            {
                return failure switch
                {
                    "excepcion" => throw new HttpRequestException("boom"),
                    "rechazo" => FakeChatClient.Json(string.Empty, ChatFinishReason.ContentFilter),
                    _ => FakeChatClient.Json("esto no es json")
                };
            }

            return FakeChatClient.Json(Products(Entry(11, complementary: S(1))));
        });

        var result = await CreateHandler(chat).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.Fill), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.BatchesFailed.Should().Be(1);
        result.Value.BatchesSucceeded.Should().Be(1);
        result.Value.RelationsCreated.Should().Be(1);
        chat.Calls.Should().HaveCount(2);

        var created = await _ctx.Set<ProductRelation>().SingleAsync();
        created.ProductId.Should().Be(products[10].Id);
        created.RelatedProductId.Should().Be(products[0].Id);
    }

    [Fact]
    public async Task ModoCompletar_OmiteTiposConDosRelacionesVisiblesAprobadas()
    {
        var a = await _seed.ProductAsync("A Shampoo");
        var b = await _seed.ProductAsync("B Guante");
        var c = await _seed.ProductAsync("C Toalla");
        var d = await _seed.ProductAsync("D Cubeta");
        await _seed.RelationAsync(a, b, ProductRelationType.Complementary);
        await _seed.RelationAsync(a, c, ProductRelationType.Complementary);

        var chat = new FakeChatClient(_ => FakeChatClient.Json(
            Products(Entry(1, complementary: S(4), similar: S(4)))));

        await CreateHandler(chat).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.Fill), CancellationToken.None);

        var created = await _ctx.Set<ProductRelation>()
            .Where(r => r.Source == ProductRelationSource.Ai && r.ProductId == a.Id)
            .ToListAsync();
        created.Should().ContainSingle()
            .Which.Should().Match<ProductRelation>(r => r.RelatedProductId == d.Id && r.Type == ProductRelationType.Similar);
    }

    [Fact]
    public async Task SinChatClient_Devuelve503()
    {
        var result = await CreateHandler(null).Handle(
            new GenerateProductRelationsCommand(ProductRelationGenerationMode.New), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.StatusCode.Should().Be(503);
        result.Error!.Message.Should().Be("La generación de recomendaciones no está configurada.");
    }
}
