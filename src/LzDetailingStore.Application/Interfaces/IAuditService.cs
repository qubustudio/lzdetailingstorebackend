namespace LzDetailingStore.Application.Interfaces;

public record AuditEntry(
    Guid? UserId,
    string Action,
    string EntityType,
    string? EntityId,
    object? OldValues,
    object? NewValues,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);

public interface IAuditService
{
    Task LogAsync(AuditEntry entry, CancellationToken ct = default);
}
