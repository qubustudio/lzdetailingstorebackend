using System.Security.Claims;
using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateProduct;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.CreateVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProduct;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteProductImage;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.DeleteVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSpecs;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.ManageProductSteps;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.SetProductActive;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.SetVariantAvailability;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateProduct;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UpdateVariant;
using LzDetailingStore.Application.Features.Catalog.Commands.Products.UploadProductImage;
using LzDetailingStore.Application.Features.Catalog.DTOs;
using LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProductById;
using LzDetailingStore.Application.Features.Catalog.Queries.GetAdminProducts;
using LzDetailingStore.Application.Features.Catalog.Queries.GetMeasurementUnits;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace LzDetailingStore.Api.Endpoints.Catalog;

public sealed class AdminProductEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        // The group only requires an authenticated user: policies stack, so a permission here would be
        // demanded on top of each route's own (e.g. operational staff toggling availability without "crear").
        var products = app.MapGroup("/api/v1/admin/productos")
            .WithTags("Admin - Products")
            .RequireAuthorization();

        // Products CRUD
        products.MapGet("/", GetProducts)
            .RequireAuthorization(AdminProductPermissions.Ver);

        products.MapGet("/{id:guid}", GetProductById)
            .RequireAuthorization(AdminProductPermissions.Ver);

        // Fixed catalog that feeds the variant size unit dropdown.
        products.MapGet("/unidades", GetMeasurementUnits)
            .RequireAuthorization(AdminProductPermissions.Ver);

        products.MapPost("/", CreateProduct)
            .RequireAuthorization("productos.crear");

        products.MapPut("/{id:guid}", UpdateProduct)
            .RequireAuthorization(AdminProductPermissions.Editar);

        products.MapPatch("/{id:guid}/estado", SetProductActive)
            .RequireAuthorization(AdminProductPermissions.Editar);

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

        // Operational staff toggle availability with only "productos.disponibilidad".
        products.MapPatch("/{productId:guid}/variantes/{variantId:guid}/disponibilidad", SetVariantAvailability)
            .RequireAuthorization("productos.disponibilidad");
    }

    private static async Task<IResult> GetProducts(
        ClaimsPrincipal user, IAuthorizationService authorization, ISender sender, CancellationToken ct,
        int page = 1, int limit = 20, string? search = null,
        Guid? brandId = null, Guid? categoryId = null, bool? activo = null,
        bool? conAgotados = null)
    {
        var incluyeCostos = await CanSeeCostsAsync(user, authorization);

        var query = new GetAdminProductsQuery(page, limit, search, brandId, categoryId, activo, incluyeCostos, conAgotados);
        var result = await sender.Send(query, ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetProductById(
        Guid id, ClaimsPrincipal user, IAuthorizationService authorization, ISender sender, CancellationToken ct)
    {
        var incluyeCostos = await CanSeeCostsAsync(user, authorization);

        var result = await sender.Send(new GetAdminProductByIdQuery(id, incluyeCostos), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetMeasurementUnits(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetMeasurementUnitsQuery(), ct);
        return result.ToApiResponse();
    }

    // Cost prices require an extra permission; the permission policy provider resolves it exactly like
    // RequireAuthorization does (Operativo browses products without seeing costs).
    private static async Task<bool> CanSeeCostsAsync(ClaimsPrincipal user, IAuthorizationService authorization) =>
        (await authorization.AuthorizeAsync(user, AdminProductPermissions.Costos)).Succeeded;

    private static async Task<IResult> SetProductActive(
        Guid id, SetProductActiveRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new SetProductActiveCommand(id, request.Activo), ct);
        return result.ToApiResponse();
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

    private static async Task<IResult> SetVariantAvailability(
        Guid productId, Guid variantId, SetVariantAvailabilityRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(
            new SetVariantAvailabilityCommand(productId, variantId, request.Disponible), ct);
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

public sealed record SetVariantAvailabilityRequest(bool Disponible);

public sealed record SetProductActiveRequest(bool Activo);
