using LzDetailingStore.Application.Features.Admin.BulkImport.DTOs;
using LzDetailingStore.Domain.Entities.Business;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Admin.BulkImport.Queries.GetImportHistory;

public sealed class GetImportHistoryQueryHandler(DbContext dbContext)
    : IRequestHandler<GetImportHistoryQuery, Result<PagedResult<ImportLogDto>>>
{
    public async Task<Result<PagedResult<ImportLogDto>>> Handle(
        GetImportHistoryQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var limit = Math.Clamp(request.Limit, 1, 50);

        var query = dbContext.Set<BulkImportLog>()
            .OrderByDescending(i => i.CreatedAt);

        var total = await query.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)total / limit);

        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(i => new ImportLogDto(
                i.Id, i.ImportType, i.FileName, i.Status.ToString(),
                i.TotalRows, i.SuccessCount, i.ErrorCount,
                i.StartedAt, i.CompletedAt, i.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ImportLogDto>(items, total, page, totalPages);
    }
}
