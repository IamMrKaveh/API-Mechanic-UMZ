namespace Application.Variant.EventHandlers;

/// <summary>
/// Handler رویداد دامنه که آن را به Application Notification تبدیل می‌کند
/// داده‌های لازم را از DB می‌خواند و یک notification enriched ارسال می‌کند
/// </summary>
public class VariantStockChangedDomainEventHandler : INotificationHandler<VariantStockChangedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<VariantStockChangedDomainEventHandler> _logger;

    public VariantStockChangedDomainEventHandler(
        IProductRepository productRepository,
        IPublisher publisher,
        ILogger<VariantStockChangedDomainEventHandler> logger)
    {
        _productRepository = productRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(VariantStockChangedEvent notification, CancellationToken cancellationToken)
    {
        var variant = await _productRepository.GetVariantByIdAsync(notification.VariantId, cancellationToken);

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

        await _publisher.Publish(appNotification, cancellationToken);
    }
}