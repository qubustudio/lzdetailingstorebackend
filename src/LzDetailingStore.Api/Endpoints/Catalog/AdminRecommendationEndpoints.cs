using System.Security.Claims;
using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Recommendations.Commands.CreateProductRelation;
using LzDetailingStore.Application.Features.Recommendations.Commands.DeleteProductRelation;
using LzDetailingStore.Application.Features.Recommendations.Commands.EnqueueProductRelationGeneration;
using LzDetailingStore.Application.Features.Recommendations.Commands.ReorderProductRelations;
using LzDetailingStore.Application.Features.Recommendations.Commands.UpdateProductRelation;
using LzDetailingStore.Application.Features.Recommendations.Queries.GetProductRelations;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Catalog;

public sealed class AdminRecommendationEndpoints : IEndpointGroup
{
    public const string ManagePermission = "recomendaciones.gestionar";

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/recomendaciones")
            .WithTags("Admin - Recommendations")
            .RequireAuthorization(ManagePermission);

        group.MapPost("/generar", Generate);
        group.MapGet("/", GetRelations);
        group.MapPost("/", CreateRelation);
        group.MapPatch("/{id:guid}", UpdateRelation);
        group.MapDelete("/{id:guid}", DeleteRelation);
        group.MapPut("/producto/{productId:guid}/orden", ReorderRelations);
    }

    private static async Task<IResult> Generate(
        GenerateRecommendationsRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new EnqueueProductRelationGenerationCommand(request.Mode), ct);
        return result.IsSuccess
            ? Results.Accepted(value: result.Value)
            : result.ToApiResponse();
    }

    private static async Task<IResult> GetRelations(
        ISender sender, CancellationToken ct,
        string? estado = null, Guid? productId = null, string? tipo = null,
        int page = 1, int pageSize = 20)
    {
        var result = await sender.Send(new GetProductRelationsQuery(estado, productId, tipo, page, pageSize), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> CreateRelation(
        CreateRecommendationRequest request, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var command = new CreateProductRelationCommand(
            request.ProductId, request.RelatedProductId, request.Tipo, request.Reason, GetUserId(ctx));

        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? result.ToCreatedResponse($"/api/v1/admin/recomendaciones/{result.Value!.Id}")
            : result.ToApiResponse();
    }

    private static async Task<IResult> UpdateRelation(
        Guid id, UpdateRecommendationRequest request, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        var command = new UpdateProductRelationCommand(id, request.Status, request.Reason, GetUserId(ctx));
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> ReorderRelations(
        Guid productId, ReorderRecommendationsRequest request, ISender sender, CancellationToken ct)
    {
        var command = new ReorderProductRelationsCommand(productId, request.Tipo, request.Ids ?? []);
        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteRelation(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProductRelationCommand(id), ct);
        return result.ToApiResponse();
    }

    private static Guid? GetUserId(HttpContext ctx)
    {
        var value = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

public sealed record GenerateRecommendationsRequest(string Mode);

public sealed record CreateRecommendationRequest(Guid ProductId, Guid RelatedProductId, string Tipo, string? Reason);

public sealed record UpdateRecommendationRequest(string? Status, string? Reason);

public sealed record ReorderRecommendationsRequest(string Tipo, IReadOnlyList<Guid>? Ids);
