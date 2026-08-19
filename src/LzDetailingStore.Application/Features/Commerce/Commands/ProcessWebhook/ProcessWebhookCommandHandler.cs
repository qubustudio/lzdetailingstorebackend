using LzDetailingStore.Application.Interfaces;
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
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            logger.LogWarning("Order not found for webhook: {OrderId}", orderId);
            return Result.Success(); // Idempotent: don't fail
        }

        // Idempotent: if already paid, return success
        if (order.Status == OrderStatus.Pagada)
            return Result.Success();

        // Process based on payment status
        if (payment.Status == "approved")
        {
            order.Status = OrderStatus.Pagada;
            order.MercadoPagoPaymentId = payment.PaymentId;

            // Clear user's cart
            var cart = await dbContext.Set<Cart>()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == order.UserId, ct);

            if (cart is not null)
                dbContext.Set<CartItem>().RemoveRange(cart.Items);

            logger.LogInformation("Order {OrderId} paid via MercadoPago payment {PaymentId}",
                orderId, payment.PaymentId);

            await unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
