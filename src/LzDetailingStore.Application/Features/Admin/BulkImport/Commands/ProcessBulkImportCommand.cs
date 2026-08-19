using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Admin.BulkImport.Commands;

public sealed record ProcessBulkImportCommand(
    Guid UserId,
    string ImportType,
    Stream FileStream,
    string FileName) : IRequest<Result<BulkImportResponseDto>>;

public sealed record BulkImportResponseDto(
    Guid ImportLogId,
    string Status,
    int? TotalRows,
    int? SuccessCount,
    int? ErrorCount);
