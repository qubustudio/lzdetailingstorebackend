using System.Text.Json;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Business;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Admin.BulkImport.Commands;

public sealed class ProcessBulkImportCommandHandler(
    DbContext dbContext,
    IBulkImportService importService,
    IS3Service s3Service,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ProcessBulkImportCommand, Result<BulkImportResponseDto>>
{
    private static readonly string[] ValidTypes = ["products", "brands", "categories"];
    private const int SyncThreshold = 100;

    public async Task<Result<BulkImportResponseDto>> Handle(
        ProcessBulkImportCommand request, CancellationToken ct)
    {
        if (!ValidTypes.Contains(request.ImportType))
            return Result<BulkImportResponseDto>.Failure(
                new Error("INVALID_TYPE", $"Tipo de importación inválido. Válidos: {string.Join(", ", ValidTypes)}."));

        // Upload file to S3
        var key = $"imports/{request.UserId}/{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid()}/{request.FileName}";
        var fileUrl = await s3Service.UploadAsync(request.FileStream, key, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ct);

        // Create import log
        var importLog = new BulkImportLog
        {
            UserId = request.UserId,
            ImportType = request.ImportType,
            FileName = request.FileName,
            FileUrl = fileUrl,
            Status = ImportStatus.Pending
        };

        await dbContext.Set<BulkImportLog>().AddAsync(importLog, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Count rows to decide sync vs async
        request.FileStream.Position = 0;
        using var workbook = new ClosedXML.Excel.XLWorkbook(request.FileStream);
        var rowCount = workbook.Worksheets.First().LastRowUsed()?.RowNumber() - 1 ?? 0;

        if (rowCount <= SyncThreshold)
        {
            // Sync processing for small files
            importLog.Status = ImportStatus.Processing;
            importLog.StartedAt = DateTime.UtcNow;

            request.FileStream.Position = 0;

            var result = request.ImportType switch
            {
                "products" => await importService.ProcessProductsAsync(request.FileStream, ct),
                "brands" => await importService.ProcessBrandsAsync(request.FileStream, ct),
                "categories" => await importService.ProcessCategoriesAsync(request.FileStream, ct),
                _ => throw new InvalidOperationException()
            };

            importLog.TotalRows = result.TotalRows;
            importLog.SuccessCount = result.SuccessCount;
            importLog.ErrorCount = result.ErrorCount;
            importLog.ErrorsDetail = result.Errors.Count > 0
                ? JsonSerializer.Serialize(result.Errors)
                : null;
            importLog.Status = ImportStatus.Completed;
            importLog.CompletedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync(ct);

            return new BulkImportResponseDto(
                importLog.Id, "completed", result.TotalRows, result.SuccessCount, result.ErrorCount);
        }

        // Async processing for large files — job will pick it up
        importLog.TotalRows = rowCount;
        await unitOfWork.SaveChangesAsync(ct);

        return new BulkImportResponseDto(importLog.Id, "pending", rowCount, null, null);
    }
}
