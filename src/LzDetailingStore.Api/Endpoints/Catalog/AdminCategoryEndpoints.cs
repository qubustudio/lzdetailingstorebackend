using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Catalog.Commands.Categories.CreateCategory;
using LzDetailingStore.Application.Features.Catalog.Commands.Categories.DeleteCategory;
using LzDetailingStore.Application.Features.Catalog.Commands.Categories.UpdateCategory;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Catalog;

public sealed class AdminCategoryEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/categorias")
            .WithTags("Admin - Categories")
            .RequireAuthorization("categorias.crear");

        group.MapPost("/", CreateCategory)
            .RequireAuthorization("categorias.crear");

        group.MapPut("/{id:guid}", UpdateCategory)
            .RequireAuthorization("categorias.editar");

        group.MapDelete("/{id:guid}", DeleteCategory)
            .RequireAuthorization("categorias.eliminar");
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse($"/api/v1/categorias");
    }

    private static async Task<IResult> UpdateCategory(
        Guid id, UpdateCategoryCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteCategory(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteCategoryCommand(id), ct);
        return result.ToApiResponse();
    }
}
