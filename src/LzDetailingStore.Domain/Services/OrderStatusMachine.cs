using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Services;

public static class OrderStatusMachine
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pendiente] = [OrderStatus.Pagada, OrderStatus.Cancelada],
        [OrderStatus.Pagada] = [OrderStatus.Enviada, OrderStatus.Cancelada],
        [OrderStatus.Enviada] = [OrderStatus.Entregada],
        [OrderStatus.Entregada] = [],
        [OrderStatus.Cancelada] = []
    };

    public static bool CanTransition(OrderStatus current, OrderStatus target)
        => Transitions.TryGetValue(current, out var allowed) && allowed.Contains(target);

    public static OrderStatus[] GetAllowedTransitions(OrderStatus current)
        => Transitions.TryGetValue(current, out var allowed) ? allowed : [];
}
