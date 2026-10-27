using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Entities.Catalog;
using LzDetailingStore.Domain.Entities.Commerce;
using LzDetailingStore.SharedKernel;
using LzDetailingStore.SharedKernel.Enums;
using LzDetailingStore.SharedKernel.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Application.Features.Commerce.Commands.ProcessWebhook;

public sealed class ProcessWebhookCommandHandler(
    DbContext dbContext,
    IMercadoPagoService mercadoPagoService,
    IUnitOfWork unitOfWork,
    ILogger<ProcessWebhookCommandHandler> logger)
    : IRequestHandler<ProcessWebhookCommand, Result>
{
    // Terminal states: once an order reaches any of these, further webhooks are
    // ignored so stock side effects are never applied twice.
    private static readonly OrderStatus[] TerminalStates =
    [
        OrderStatus.Pagada,
        OrderStatus.Enviada,
        OrderStatus.Entregada,
        OrderStatus.Cancelada,
        OrderStatus.Rechazada,
        OrderStatus.Reembolsada
    ];

    public async Task<Result> Handle(ProcessWebhookCommand request, CancellationToken ct)
    {
        // Only process payment notifications
        if (request.Type != "payment")
            return Result.Success();

        // Validate webhook signature
        if (!mercadoPagoService.ValidateWebhookSignature(
            request.XSignature, request.XRequestId, request.DataId))
        {
            logger.LogWarning("Invalid MercadoPago webhook signature for DataId: {DataId}", request.DataId);
            return Result.Failure(new Error("INVALID_SIGNATURE", "Firma inválida."), 401);
        }

        // Get payment info from MercadoPago
        var paymentResult = await mercadoPagoService.GetPaymentAsync(request.DataId, ct);

        if (paymentResult.IsFailure)
            return Result.Failure(paymentResult.Error!);

        var payment = paymentResult.Value!;

        // Find order by external reference (order ID)
        if (!Guid.TryParse(payment.ExternalReference, out var orderId))
        {
            logger.LogWarning("Invalid external_reference: {Ref}", payment.ExternalReference);
            return Result.Success(); // Don't fail, just ignore
        }

        var order = await dbContext.Set<Order>()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            logger.LogWarning("Order not found for webhook: {OrderId}", orderId);
            return Result.Success(); // Idempotent: don't fail
        }

        // Idempotent guard: if the order is already in a terminal state, a prior
        // webhook already processed it (including any stock side effects). Skip.
        if (TerminalStates.Contains(order.Status))
            return Result.Success();

        order.MercadoPagoPaymentId = payment.PaymentId;

        // Map MercadoPago payment status to our order status. Stock side effects
        // run exactly once here because the order is still non-terminal (the guard
        // above and the xmin concurrency token together prevent double application).
        switch (payment.Status)
        {
            case "approved":
                order.Status = OrderStatus.Pagada;
                await DecrementStockAsync(order, ct);
                await ClearUserCartAsync(order.UserId, ct);
                logger.LogInformation("Order {OrderId} paid via MercadoPago payment {PaymentId}",
                    orderId, payment.PaymentId);
                break;

            case "rejected":
                order.Status = OrderStatus.Rechazada;
                logger.LogInformation("Order {OrderId} rejected (MP status: {Status})",
                    orderId, payment.Status);
                break;

            case "cancelled":
                order.Status = OrderStatus.Cancelada;
                logger.LogInformation("Order {OrderId} cancelled (MP status: {Status})",
                    orderId, payment.Status);
                break;

            case "refunded":
            case "charged_back":
                order.Status = OrderStatus.Reembolsada;
                await RestoreStockAsync(order, ct);
                logger.LogInformation("Order {OrderId} refunded (MP status: {Status})",
                    orderId, payment.Status);
                break;

            default:
                // pending, in_process, authorized, etc. — leave as Pendiente.
                // No stock side effects; only the payment id is recorded.
                logger.LogInformation("Order {OrderId} pending (MP status: {Status})",
                    orderId, payment.Status);
                break;
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent webhook already transitioned this order. The other
            // request owns the state change and its stock side effects; treat
            // this one as a successful no-op to stay idempotent.
            logger.LogInformation(
                "Concurrent webhook detected for order {OrderId}; skipping duplicate processing.",
                orderId);
            return Result.Success();
        }

        return Result.Success();
    }

    private async Task DecrementStockAsync(Order order, CancellationToken ct)
    {
        var variants = await LoadVariantsAsync(order, ct);

        foreach (var item in order.Items)
            if (variants.TryGetValue(item.VariantId, out var variant))
                variant.Stock = Math.Max(0, variant.Stock - item.Quantity);
    }

    private async Task RestoreStockAsync(Order order, CancellationToken ct)
    {
        // On refund/chargeback, return the reserved units back to stock.
        var variants = await LoadVariantsAsync(order, ct);

        foreach (var item in order.Items)
            if (variants.TryGetValue(item.VariantId, out var variant))
                variant.Stock += item.Quantity;
    }

    private async Task<Dictionary<Guid, ProductVariant>> LoadVariantsAsync(
        Order order, CancellationToken ct)
    {
        var variantIds = order.Items.Select(i => i.VariantId).ToList();

        return await dbContext.Set<ProductVariant>()
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);
    }

    private async Task ClearUserCartAsync(Guid userId, CancellationToken ct)
    {
        var cart = await dbContext.Set<Cart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);

        if (cart is not null)
            dbContext.Set<CartItem>().RemoveRange(cart.Items);
    }
}
