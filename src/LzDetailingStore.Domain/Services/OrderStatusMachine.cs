using LzDetailingStore.SharedKernel.Enums;

namespace LzDetailingStore.Domain.Services;

/// <summary>
/// Order status transitions an admin may perform manually. Single source of truth for both the admin
/// status update and the <c>allowedTransitions</c> exposed in the admin order detail.
/// <para>
/// <see cref="OrderStatus.Pagada"/> is intentionally unreachable here: payments are confirmed only by the
/// Mercado Pago webhook, which sets the status directly and does not go through these rules.
/// </para>
/// </summary>
public static class OrderStatusMachine
{
    public const string PaymentOnlyViaMercadoPagoMessage = "El pago solo se confirma desde Mercado Pago.";

    private static readonly Dictionary<OrderStatus, OrderStatus[]> Transitions = new()
    {
        [OrderStatus.Pendiente] = [OrderStatus.Cancelada],
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
