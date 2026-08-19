using LzDetailingStore.Api.Extensions;
using LzDetailingStore.Application.Features.Admin.BulkImport.Commands;
using LzDetailingStore.Application.Features.Admin.BulkImport.Queries.GetImportErrors;
using LzDetailingStore.Application.Features.Admin.BulkImport.Queries.GetImportHistory;
using MediatR;
using System.Security.Claims;

namespace LzDetailingStore.Api.Endpoints.Admin;

public sealed class ImportEndpoints : IEndpointGroup
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/importar")
            .WithTags("Admin - Import")
            .RequireAuthorization("productos.crear");

        group.MapPost("/{tipo}", UploadImport).DisableAntiforgery();
        group.MapGet("/historial", GetHistory);
        group.MapGet("/{id:guid}/errores", GetErrors);
    }

    private static async Task<IResult> UploadImport(
        string tipo, IFormFile file, HttpContext ctx, ISender sender, CancellationToken ct)
    {
        if (file.Length == 0)
            return Results.BadRequest(new { error = "EMPTY_FILE", message = "El archivo esta vacio." });

        var userId = Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? ctx.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException());

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        var command = new ProcessBulkImportCommand(userId, tipo, memoryStream, file.FileName);
        var result = await sender.Send(command, ct);

        if (result.IsFailure)
            return result.ToApiResponse();

        return result.Value!.Status == "completed"
            ? Results.Ok(result.Value)
            : Results.Accepted($"/api/v1/admin/importar/{result.Value.ImportLogId}/errores", result.Value);
    }

    private static async Task<IResult> GetHistory(
        ISender sender, CancellationToken ct, int page = 1, int limit = 20)
    {
        var result = await sender.Send(new GetImportHistoryQuery(page, limit), ct);
        return result.ToApiResponse();
    }

    private static async Task<IResult> GetErrors(
        Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetImportErrorsQuery(id), ct);
        return result.ToApiResponse();
    }
}
