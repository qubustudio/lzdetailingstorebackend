using System.Text.Json;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Business;
using LzDetailingStore.SharedKernel;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LzDetailingStore.Application.Features.Admin.BulkImport.Queries.GetImportErrors;

public sealed class GetImportErrorsQueryHandler(DbContext dbContext)
    : IRequestHandler<GetImportErrorsQuery, Result<ImportErrorsDto>>
{
    public async Task<Result<ImportErrorsDto>> Handle(GetImportErrorsQuery request, CancellationToken ct)
    {
        var importLog = await dbContext.Set<BulkImportLog>()
            .FirstOrDefaultAsync(i => i.Id == request.ImportLogId, ct);

        if (importLog is null)
            return Result<ImportErrorsDto>.NotFound("Registro de importación no encontrado.");

        var errors = string.IsNullOrWhiteSpace(importLog.ErrorsDetail)
            ? []
            : JsonSerializer.Deserialize<List<BulkImportError>>(importLog.ErrorsDetail) ?? [];

        return new ImportErrorsDto(
            importLog.Id, importLog.ImportType, importLog.Status.ToString(),
            importLog.TotalRows, importLog.SuccessCount, importLog.ErrorCount, errors);
    }
}
