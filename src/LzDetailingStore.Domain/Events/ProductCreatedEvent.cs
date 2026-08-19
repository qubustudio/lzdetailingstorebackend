using LzDetailingStore.SharedKernel;

namespace LzDetailingStore.Domain.Events;

public sealed record ProductCreatedEvent(Guid ProductId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
