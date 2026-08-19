using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Catalog.Commands.Brands.CreateBrand;
using LzDetailingStore.Application.Features.Catalog.Commands.Brands.DeleteBrand;
using LzDetailingStore.Application.Features.Catalog.Commands.Brands.UpdateBrand;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Catalog;

public sealed class AdminBrandEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/marcas")
            .WithTags("Admin - Brands")
            .RequireAuthorization("marcas.crear");

        group.MapPost("/", CreateBrand)
            .RequireAuthorization("marcas.crear");

        group.MapPut("/{id:guid}", UpdateBrand)
            .RequireAuthorization("marcas.editar");

        group.MapDelete("/{id:guid}", DeleteBrand)
            .RequireAuthorization("marcas.eliminar");
    }

    private static async Task<IResult> CreateBrand(
        CreateBrandCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse($"/api/v1/marcas");
    }

    private static async Task<IResult> UpdateBrand(
        Guid id, UpdateBrandCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteBrand(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteBrandCommand(id), ct);
        return result.ToApiResponse();
    }
}
