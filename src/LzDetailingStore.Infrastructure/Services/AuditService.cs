using System.Text.Json;
using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Business;
using LzDetailingStore.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Infrastructure.Services;

public sealed class AuditService(
    AppDbContext dbContext,
    ILogger<AuditService> logger) : IAuditService
{
    public async Task LogAsync(AuditEntry entry, CancellationToken ct = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = entry.UserId,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                OldValues = entry.OldValues is not null ? JsonSerializer.Serialize(entry.OldValues) : null,
                NewValues = entry.NewValues is not null ? JsonSerializer.Serialize(entry.NewValues) : null,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                CorrelationId = entry.CorrelationId
            };

            await dbContext.Set<AuditLog>().AddAsync(auditLog, ct);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for {Action} on {EntityType}/{EntityId}",
                entry.Action, entry.EntityType, entry.EntityId);
        }
    }
}
