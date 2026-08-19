namespace LzDetailingStore.Application.Features.Admin.BulkImport.DTOs;

public sealed record ImportLogDto(
    Guid Id,
    string ImportType,
    string FileName,
    string Status,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
