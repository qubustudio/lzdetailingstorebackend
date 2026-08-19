using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Entities.Business;

public sealed class BulkImportLog : AuditableEntity
{
    public Guid UserId { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FileUrl { get; set; }
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorsDetail { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
