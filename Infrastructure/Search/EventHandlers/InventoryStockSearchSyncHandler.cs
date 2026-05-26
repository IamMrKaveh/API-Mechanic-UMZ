using Domain.Inventory.Events;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Search.EventHandlers;

public sealed class InventoryStockSearchSyncHandler(DBContext context) :
    INotificationHandler<DomainEventNotification<StockIncreasedEvent>>,
    INotificationHandler<DomainEventNotification<StockReservedEvent>>,
    INotificationHandler<DomainEventNotification<StockReservationReleasedEvent>>
{
    public async Task Handle(DomainEventNotification<StockIncreasedEvent> notification, CancellationToken ct)
        => await EnqueueProductUpdate(notification.DomainEvent.VariantId, ct);

    public async Task Handle(DomainEventNotification<StockReservedEvent> notification, CancellationToken ct)
        => await EnqueueProductUpdate(notification.DomainEvent.VariantId, ct);

    public async Task Handle(DomainEventNotification<StockReservationReleasedEvent> notification, CancellationToken ct)
        => await EnqueueProductUpdate(notification.DomainEvent.VariantId, ct);

    private async Task EnqueueProductUpdate(VariantId variantId, CancellationToken ct)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => new { v.ProductId })
            .FirstOrDefaultAsync(ct);

        if (variant is null) return;

        var message = ElasticsearchOutboxMessage.Create(
            "Product",
            variant.ProductId.Value,
            JsonSerializer.Serialize(new { ProductId = variant.ProductId.Value }),
            "StockChanged");

        await context.ElasticsearchOutboxMessages.AddAsync(message, ct);
    }
}