using Domain.Inventory.Events;

namespace Infrastructure.Cache.EventHandlers;

/// <summary>
/// هنگام تغییر موجودی، کش Variant مربوطه را Invalidate می‌کند.
/// </summary>
public sealed class InventoryStockChangedCacheHandler(ICacheInvalidationService invalidation) :
    INotificationHandler<StockReservedEvent>,
    INotificationHandler<StockReleasedEvent>,
    INotificationHandler<StockCommittedEvent>,
    INotificationHandler<AdjustStockEvent>,
    INotificationHandler<StockReturnedEvent>
{
    private readonly ICacheInvalidationService _invalidation = invalidation;

    public Task Handle(StockReservedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockReleasedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockCommittedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(AdjustStockEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    public Task Handle(StockReturnedEvent n, CancellationToken ct) => Invalidate(n.VariantId, ct);

    private async Task Invalidate(int variantId, CancellationToken ct)
    {
        await _invalidation.InvalidateInventoryAsync(variantId, ct);
        _logger.LogDebug("[CacheEvent] Inventory invalidated for Variant {VariantId}", variantId);
    }
}