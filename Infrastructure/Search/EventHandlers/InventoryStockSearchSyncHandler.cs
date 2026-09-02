using Domain.Inventory.Events;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Search.EventHandlers;

public sealed class InventoryStockSearchSyncHandler(DBContext context) :
    INotificationHandler<DomainEventNotification<StockIncreasedEvent>>,
    INotificationHandler<DomainEventNotification<StockReservedEvent>>,
    INotificationHandler<DomainEventNotification<StockReservationReleasedEvent>>
{
    public Task Handle(
        DomainEventNotification<StockIncreasedEvent> notification,
        CancellationToken cancellationToken)
        => EnqueueAsync(
            notification.DomainEvent.VariantId,
            notification.DomainEvent.InventoryId.Value,
            $"increased:{notification.DomainEvent.NewStockQuantity}",
            cancellationToken);

    public Task Handle(
        DomainEventNotification<StockReservedEvent> notification,
        CancellationToken cancellationToken)
        => EnqueueAsync(
            notification.DomainEvent.VariantId,
            notification.DomainEvent.InventoryId.Value,
            $"reserved:{notification.DomainEvent.TotalReservedQuantity}",
            cancellationToken);

    public Task Handle(
        DomainEventNotification<StockReservationReleasedEvent> notification,
        CancellationToken cancellationToken)
        => EnqueueAsync(
            notification.DomainEvent.VariantId,
            notification.DomainEvent.InventoryId.Value,
            $"released:{notification.DomainEvent.TotalReservedQuantity}",
            cancellationToken);

    private async Task EnqueueAsync(
        VariantId variantId,
        Guid inventoryId,
        string operationTag,
        CancellationToken cancellationToken)
    {
        var productId = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => v.ProductId)
            .FirstOrDefaultAsync(cancellationToken);

        if (productId is null)
            return;

        var discriminator = $"{inventoryId:N}:{operationTag}:{Guid.NewGuid():N}";

        var document = JsonSerializer.Serialize(new
        {
            productId = productId.Value,
            variantId = variantId.Value,
            inventoryId,
            operation = operationTag
        });

        var message = ElasticsearchOutboxMessage.Create(
            entityType: "Product",
            entityId: productId.Value,
            document: document,
            changeType: "StockChanged",
            discriminator: discriminator);

        await context.ElasticsearchOutboxMessages.AddAsync(message, cancellationToken);
    }
}
