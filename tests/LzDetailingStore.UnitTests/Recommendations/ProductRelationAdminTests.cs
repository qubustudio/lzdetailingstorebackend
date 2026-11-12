using FluentAssertions;
using LzDetailingStore.Application.Features.Recommendations;
using LzDetailingStore.Application.Features.Recommendations.Commands.CreateProductRelation;
using LzDetailingStore.Application.Features.Recommendations.Commands.EnqueueProductRelationGeneration;
using LzDetailingStore.Application.Features.Recommendations.Commands.ReorderProductRelations;
using LzDetailingStore.Application.Features.Recommendations.Commands.UpdateProductRelation;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LzDetailingStore.UnitTests.Recommendations;

public sealed class ProductRelationAdminTests
{
    private readonly CatalogTestContext _ctx = CatalogTestContext.Create();
    private readonly CatalogSeeder _seed;

    public ProductRelationAdminTests() => _seed = new CatalogSeeder(_ctx);

    [Fact]
    public async Task Reordenar_RechazaIdsDeOtroProductoOTipo()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var other = await _seed.ProductAsync("Otro");
        var a = await _seed.ProductAsync("A");
        var b = await _seed.ProductAsync("B");
        var own = await _seed.RelationAsync(p, a, ProductRelationType.Complementary, sortOrder: 0);
        var otherType = await _seed.RelationAsync(p, b, ProductRelationType.Similar, sortOrder: 0);
        var foreign = await _seed.RelationAsync(other, a, ProductRelationType.Complementary, sortOrder: 0);

        var handler = new ReorderProductRelationsCommandHandler(_ctx, new PassthroughUnitOfWork(_ctx));

        var withForeign = await handler.Handle(
            new ReorderProductRelationsCommand(p.Id, ProductRelationLabels.Complementary, [own.Id, foreign.Id]),
            CancellationToken.None);
        var withOtherType = await handler.Handle(
            new ReorderProductRelationsCommand(p.Id, ProductRelationLabels.Complementary, [otherType.Id, own.Id]),
            CancellationToken.None);

        withForeign.IsFailure.Should().BeTrue();
        withForeign.Error!.Code.Should().Be("INVALID_RELATION_IDS");
        withForeign.StatusCode.Should().Be(400);
        withOtherType.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Reordenar_ReescribeSortOrderEnElOrdenIndicado()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var r1 = await _seed.RelationAsync(p, await _seed.ProductAsync("A"), ProductRelationType.Similar, sortOrder: 0);
        var r2 = await _seed.RelationAsync(p, await _seed.ProductAsync("B"), ProductRelationType.Similar, sortOrder: 1);
        var r3 = await _seed.RelationAsync(p, await _seed.ProductAsync("C"), ProductRelationType.Similar, sortOrder: 2);

        var result = await new ReorderProductRelationsCommandHandler(_ctx, new PassthroughUnitOfWork(_ctx)).Handle(
            new ReorderProductRelationsCommand(p.Id, ProductRelationLabels.Similar, [r3.Id, r1.Id]),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var order = await _ctx.Set<ProductRelation>().OrderBy(r => r.SortOrder).Select(r => r.Id).ToListAsync();
        order.Should().Equal(r3.Id, r1.Id, r2.Id);
    }

    [Fact]
    public async Task Actualizar_AprobarRegistraRevisor()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var relation = await _seed.RelationAsync(p, await _seed.ProductAsync("A"),
            ProductRelationType.Complementary, ProductRelationStatus.Draft);
        var reviewer = Guid.NewGuid();

        var result = await new UpdateProductRelationCommandHandler(_ctx, new PassthroughUnitOfWork(_ctx)).Handle(
            new UpdateProductRelationCommand(relation.Id, ProductRelationLabels.Approved, "Nueva razón", reviewer),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ProductRelationLabels.Approved);
        relation.ReviewedBy.Should().Be(reviewer);
        relation.ReviewedAt.Should().NotBeNull();
        relation.Reason.Should().Be("Nueva razón");
    }

    [Fact]
    public async Task CrearManual_QuedaAprobada_YRechazaDuplicados()
    {
        var p = await _seed.ProductAsync("Producto Base");
        var a = await _seed.ProductAsync("A");
        var handler = new CreateProductRelationCommandHandler(_ctx, new PassthroughUnitOfWork(_ctx));
        var command = new CreateProductRelationCommand(p.Id, a.Id, ProductRelationLabels.Complementary, "Razón");

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        first.Value!.Status.Should().Be(ProductRelationLabels.Approved);
        first.Value.Source.Should().Be(ProductRelationLabels.Manual);
        second.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Encolar_SinChatClientConfigurado_Devuelve503()
    {
        var scheduler = Substitute.For<IProductRelationJobScheduler>();

        var result = await new EnqueueProductRelationGenerationCommandHandler(scheduler)
            .Handle(new EnqueueProductRelationGenerationCommand(ProductRelationLabels.ModeNew), CancellationToken.None);

        result.StatusCode.Should().Be(503);
        scheduler.DidNotReceiveWithAnyArgs().EnqueueGeneration(default);
    }

    [Fact]
    public async Task Encolar_ConChatClient_DevuelveJobId()
    {
        var scheduler = Substitute.For<IProductRelationJobScheduler>();
        scheduler.EnqueueGeneration(ProductRelationGenerationMode.Fill).Returns("job-42");
        var chat = new FakeChatClient(_ => throw new InvalidOperationException("must not be called"));

        var result = await new EnqueueProductRelationGenerationCommandHandler(scheduler, chat)
            .Handle(new EnqueueProductRelationGenerationCommand("completar"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.JobId.Should().Be("job-42");
    }

    [Theory]
    [InlineData("nuevos", true)]
    [InlineData("completar", true)]
    [InlineData("todos", false)]
    [InlineData("", false)]
    public void ValidadorEncolar_AceptaSoloModosValidos(string mode, bool valid)
    {
        var validation = new EnqueueProductRelationGenerationCommandValidator()
            .Validate(new EnqueueProductRelationGenerationCommand(mode));

        validation.IsValid.Should().Be(valid);
    }
}
