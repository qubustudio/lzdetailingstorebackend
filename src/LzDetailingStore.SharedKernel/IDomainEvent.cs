using MediatR;

namespace LzDetailingStore.SharedKernel;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
