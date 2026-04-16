using Domain.Inventory.Events;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

/// <summary>
/// هنگام تغییر موجودی، کش Variant مربوطه را Invalidate می‌کند.
/// </summary>
public sealed class InventoryStockChangedCacheHandler(
    ICacheInvalidationService invalidation,
    IAuditService auditService) :

    INotificationHandler<StockReservedEvent>,
    INotificationHandler<StockReleasedEvent>,
    INotificationHandler<StockCommittedEvent>,
    INotificationHandler<StockAdjustedEvent>,
    INotificationHandler<StockReturnedEvent>
{
    public Task Handle(StockReservedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockReleasedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockCommittedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockAdjustedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockReturnedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    private async Task Invalidate(VariantId variantId, CancellationToken ct)
    {
        await invalidation.InvalidateInventoryCacheAsync(variantId, ct);
        await auditService.LogSystemEventAsync(
            "CacheEvent",
            $"Inventory invalidated for Variant {variantId.Value}",
            ct);
    }
}