using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Application.Features.Intelligence.Events;

public sealed class ProductUpdatedEventHandler(
    IEmbeddingJobService embeddingJobService,
    ILogger<ProductUpdatedEventHandler> logger)
    : INotificationHandler<ProductUpdatedEvent>
{
    public async Task Handle(ProductUpdatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Regenerating embedding for updated product {ProductId}", notification.ProductId);
        await embeddingJobService.RegenerateEmbeddingAsync(notification.ProductId, ct);
    }
}
