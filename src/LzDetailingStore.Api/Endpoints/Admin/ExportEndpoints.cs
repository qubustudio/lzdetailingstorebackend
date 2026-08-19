using LzDetailingStore.Application.Interfaces;

namespace LzDetailingStore.Api.Endpoints.Admin;

public sealed class ExportEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/exportar")
            .WithTags("Admin - Export")
            .RequireAuthorization("productos.ver");

        group.MapGet("/{tipo}", ExportData);

        var templates = app.MapGroup("/api/v1/admin/importar/plantilla")
            .WithTags("Admin - Import")
            .RequireAuthorization("productos.crear");

        templates.MapGet("/{tipo}", GetTemplate);
    }

    private static async Task<IResult> ExportData(
        string tipo, IBulkExportService exportService, CancellationToken ct)
    {
        var bytes = tipo switch
        {
            "products" => await exportService.ExportProductsAsync(ct),
            "brands" => await exportService.ExportBrandsAsync(ct),
            "categories" => await exportService.ExportCategoriesAsync(ct),
            _ => null
        };

        if (bytes is null)
            return Results.BadRequest(new { error = "INVALID_TYPE", message = "Tipo invalido. Validos: products, brands, categories." });

        var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var fileName = $"{tipo}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

        return Results.File(bytes, contentType, fileName);
    }

    private static IResult GetTemplate(string tipo, IBulkExportService exportService)
    {
        try
        {
            var bytes = exportService.GetTemplate(tipo);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = $"plantilla_{tipo}.xlsx";
            return Results.File(bytes, contentType, fileName);
        }
        catch (ArgumentException)
        {
            return Results.BadRequest(new { error = "INVALID_TYPE", message = "Tipo invalido. Validos: products, brands, categories." });
        }
    }
}
