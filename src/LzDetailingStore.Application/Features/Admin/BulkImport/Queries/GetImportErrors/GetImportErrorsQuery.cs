using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Admin.BulkImport.Queries.GetImportErrors;

public sealed record GetImportErrorsQuery(Guid ImportLogId)
    : IRequest<Result<ImportErrorsDto>>;

public sealed record ImportErrorsDto(
    Guid ImportLogId,
    string ImportType,
    string Status,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    List<BulkImportError> Errors);
