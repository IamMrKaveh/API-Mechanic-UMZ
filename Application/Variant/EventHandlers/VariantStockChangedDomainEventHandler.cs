using Application.Variant.Features.Shared;
using Domain.Variant.Events;
using Domain.Variant.Interfaces;

namespace Application.Variant.EventHandlers;

public class VariantStockChangedDomainEventHandler(
    IVariantRepository variantRepository,
    IPublisher publisher,
    ILogger<VariantStockChangedDomainEventHandler> logger) : INotificationHandler<VariantStockChangedEvent>
{
    public async Task Handle(VariantStockChangedEvent notification, CancellationToken ct)
    {
        var variant = await variantRepository.GetWithProductAsync(notification.VariantId, ct);

        if (variant is null)
        {
            logger.LogWarning(
                "Variant {VariantId} not found when handling VariantStockChangedEvent",
                notification.VariantId);
            return;
        }

        var appNotification = new VariantStockChangedApplicationNotification
        {
            VariantId = notification.VariantId,
            ProductId = notification.ProductId,
            QuantityChanged = notification.QuantityChanged,
            NewOnHand = variant.StockQuantity,
            NewReserved = variant.ReservedQuantity,
            NewAvailable = variant.AvailableStock,
            IsInStock = variant.IsInStock
        };

        await publisher.Publish(appNotification, ct);
    }
}