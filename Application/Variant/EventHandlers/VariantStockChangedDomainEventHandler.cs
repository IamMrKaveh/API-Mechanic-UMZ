using Domain.Variant.Interfaces;

namespace Application.Variant.EventHandlers;

public class VariantStockChangedDomainEventHandler(
    IVariantRepository variantRepository,
    IPublisher publisher,
    ILogger<VariantStockChangedDomainEventHandler> logger) : INotificationHandler<VariantStockChangedEvent>
{
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<VariantStockChangedDomainEventHandler> _logger = logger;

    public async Task Handle(VariantStockChangedEvent notification, CancellationToken ct)
    {
        var variant = await _variantRepository.GetWithProductAsync(notification.VariantId, ct);

        if (variant == null)
        {
            _logger.LogWarning(
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

        await _publisher.Publish(appNotification, ct);
    }
}