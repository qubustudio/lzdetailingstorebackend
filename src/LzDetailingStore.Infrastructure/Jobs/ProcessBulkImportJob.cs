using System.Text.Json;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Business;
using LzDetailingStore.Infrastructure.Persistence;
using LzDetailingStore.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Jobs;

public sealed class ProcessBulkImportJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessBulkImportJob> logger)
{
    public async Task ExecuteAsync(Guid importLogId, CancellationToken ct = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importService = scope.ServiceProvider.GetRequiredService<IBulkImportService>();
        var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();

        var importLog = await dbContext.Set<BulkImportLog>()
            .FirstOrDefaultAsync(i => i.Id == importLogId, ct);

        if (importLog is null)
        {
            logger.LogWarning("Import log {ImportLogId} not found", importLogId);
            return;
        }

        importLog.Status = ImportStatus.Processing;
        importLog.StartedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(ct);

        try
        {
            // Download file from S3
            using var httpClient = new HttpClient();
            await using var fileStream = await httpClient.GetStreamAsync(importLog.FileUrl, ct);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            var result = importLog.ImportType switch
            {
                "products" => await importService.ProcessProductsAsync(memoryStream, ct),
                "brands" => await importService.ProcessBrandsAsync(memoryStream, ct),
                "categories" => await importService.ProcessCategoriesAsync(memoryStream, ct),
                _ => throw new ArgumentException($"Unknown import type: {importLog.ImportType}")
            };

            importLog.TotalRows = result.TotalRows;
            importLog.SuccessCount = result.SuccessCount;
            importLog.ErrorCount = result.ErrorCount;
            importLog.ErrorsDetail = result.Errors.Count > 0
                ? JsonSerializer.Serialize(result.Errors)
                : null;
            importLog.Status = ImportStatus.Completed;
            importLog.CompletedAt = DateTime.UtcNow;

            logger.LogInformation(
                "Import {ImportLogId} completed: {Success}/{Total} rows, {Errors} errors",
                importLogId, result.SuccessCount, result.TotalRows, result.ErrorCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import {ImportLogId} failed", importLogId);

            importLog.Status = ImportStatus.Failed;
            importLog.ErrorsDetail = JsonSerializer.Serialize(new[]
            {
                new { Row = 0, Field = "System", Message = ex.Message }
            });
            importLog.CompletedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
