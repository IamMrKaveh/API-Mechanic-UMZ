using Domain.Inventory.Events;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

public sealed class InventoryStockChangedCacheHandler(
    ICacheInvalidationService invalidation,
    IAuditService auditService) :
    INotificationHandler<DomainEventNotification<StockReservedEvent>>,
    INotificationHandler<DomainEventNotification<StockReleasedEvent>>,
    INotificationHandler<DomainEventNotification<StockCommittedEvent>>,
    INotificationHandler<DomainEventNotification<StockAdjustedEvent>>,
    INotificationHandler<DomainEventNotification<StockReturnedEvent>>
{
    public Task Handle(DomainEventNotification<StockReservedEvent> n, CancellationToken ct)
        => Invalidate(n.DomainEvent.VariantId, ct);

    public Task Handle(DomainEventNotification<StockReleasedEvent> n, CancellationToken ct)
        => Invalidate(n.DomainEvent.VariantId, ct);

    public Task Handle(DomainEventNotification<StockCommittedEvent> n, CancellationToken ct)
        => Invalidate(n.DomainEvent.VariantId, ct);

    public Task Handle(DomainEventNotification<StockAdjustedEvent> n, CancellationToken ct)
        => Invalidate(n.DomainEvent.VariantId, ct);

    public Task Handle(DomainEventNotification<StockReturnedEvent> n, CancellationToken ct)
        => Invalidate(n.DomainEvent.VariantId, ct);

    private async Task Invalidate(VariantId variantId, CancellationToken ct)
    {
        await invalidation.InvalidateInventoryCacheAsync(variantId, ct);
        await auditService.LogSystemEventAsync(
            "CacheEvent",
            $"Inventory invalidated for Variant {variantId.Value}",
            ct);
    }
}