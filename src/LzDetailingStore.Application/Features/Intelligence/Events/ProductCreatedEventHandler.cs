using LzDetailingStore.Application.Interfaces;
using LzDetailingStore.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LzDetailingStore.Application.Features.Intelligence.Events;

public sealed class ProductCreatedEventHandler(
    IEmbeddingJobService embeddingJobService,
    ILogger<ProductCreatedEventHandler> logger)
    : INotificationHandler<ProductCreatedEvent>
{
    public async Task Handle(ProductCreatedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Generating embedding for new product {ProductId}", notification.ProductId);
        await embeddingJobService.RegenerateEmbeddingAsync(notification.ProductId, ct);
    }
}
