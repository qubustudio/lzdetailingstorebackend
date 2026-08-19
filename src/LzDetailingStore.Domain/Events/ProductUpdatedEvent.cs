using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Events;

public sealed record ProductUpdatedEvent(Guid ProductId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
