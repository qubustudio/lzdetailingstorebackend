using LzDetailingStore.Application.Features.Admin.BulkImport.DTOs;
using LzDetailingStore.SharedKernel;
using MediatR;

namespace LzDetailingStore.Application.Features.Admin.BulkImport.Queries.GetImportHistory;

public sealed record GetImportHistoryQuery(int Page = 1, int Limit = 20)
    : IRequest<Result<PagedResult<ImportLogDto>>>;
