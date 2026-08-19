using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateProduct;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProduct;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProductImage;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSpecs;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSteps;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateProduct;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UploadProductImage;
using MediatR;

namespace LzDetailingStore.Api.Endpoints.Catalog;

public sealed class AdminProductEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/v1/admin/productos")
            .WithTags("Admin - Products")
            .RequireAuthorization("productos.crear");

        // Products CRUD
        products.MapPost("/", CreateProduct)
            .RequireAuthorization("productos.crear");

        products.MapPut("/{id:guid}", UpdateProduct)
            .RequireAuthorization("productos.editar");

        products.MapDelete("/{id:guid}", DeleteProduct)
            .RequireAuthorization("productos.eliminar");

        // Variants
        products.MapPost("/{productId:guid}/variantes", CreateVariant)
            .RequireAuthorization("productos.crear");

        products.MapPut("/variantes/{id:guid}", UpdateVariant)
            .RequireAuthorization("productos.editar");

        products.MapDelete("/variantes/{id:guid}", DeleteVariant)
            .RequireAuthorization("productos.eliminar");

        // Images
        products.MapPost("/{productId:guid}/imagenes", UploadImage)
            .RequireAuthorization("productos.editar")
            .DisableAntiforgery();

        products.MapDelete("/imagenes/{id:guid}", DeleteImage)
            .RequireAuthorization("productos.eliminar");

        // Specs & Steps
        products.MapPut("/{productId:guid}/specs", ManageSpecs)
            .RequireAuthorization("productos.editar");

        products.MapPut("/{productId:guid}/pasos", ManageSteps)
            .RequireAuthorization("productos.editar");
    }

    private static async Task<IResult> CreateProduct(
        CreateProductCommand command, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse("/api/v1/productos");
    }

    private static async Task<IResult> UpdateProduct(
        Guid id, UpdateProductCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteProduct(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProductCommand(id), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> CreateVariant(
        Guid productId, CreateVariantCommand command, ISender sender, CancellationToken ct)
    {
        if (productId != command.ProductId)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ProductId de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse($"/api/v1/admin/productos/{productId}/variantes");
    }

    private static async Task<IResult> UpdateVariant(
        Guid id, UpdateVariantCommand command, ISender sender, CancellationToken ct)
    {
        if (id != command.Id)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ID de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> DeleteVariant(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteVariantCommand(id), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> UploadImage(
        Guid productId, IFormFile file, ISender sender, CancellationToken ct,
        string? imageAlt = null, int orden = 0)
    {
        if (file.Length == 0)
            return Results.BadRequest(new { error = "EMPTY_FILE", message = "El archivo está vacío." });

        await using var stream = file.OpenReadStream();
        var command = new UploadProductImageCommand(
            productId, stream, file.FileName, file.ContentType, imageAlt, orden);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResponse($"/api/v1/admin/productos/{productId}/imagenes");
    }

    private static async Task<IResult> DeleteImage(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteProductImageCommand(id), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> ManageSpecs(
        Guid productId, ManageProductSpecsCommand command, ISender sender, CancellationToken ct)
    {
        if (productId != command.ProductId)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ProductId de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> ManageSteps(
        Guid productId, ManageProductStepsCommand command, ISender sender, CancellationToken ct)
    {
        if (productId != command.ProductId)
            return Results.BadRequest(new { error = "ID_MISMATCH", message = "El ProductId de la ruta no coincide con el del cuerpo." });

        var result = await sender.Send(command, ct);
        return result.ToApiResponse();
    }
}
