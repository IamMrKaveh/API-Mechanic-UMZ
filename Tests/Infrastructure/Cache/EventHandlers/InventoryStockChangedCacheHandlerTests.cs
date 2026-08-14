using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Events;
using Domain.Inventory.Events;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Cache.EventHandlers;

namespace Tests.Infrastructure.Cache.EventHandlers;

public class InventoryStockChangedCacheHandlerTests
{
    private readonly ICacheInvalidationService _invalidation = Substitute.For<ICacheInvalidationService>(); private readonly IAuditService _audit = Substitute.For<IAuditService>(); private readonly InventoryStockChangedCacheHandler _sut;

    public InventoryStockChangedCacheHandlerTests()
    {
        _sut = new InventoryStockChangedCacheHandler(_invalidation, _audit);
    }

    [Fact]
    public async Task Handle_StockReservedEvent_InvalidatesInventoryCacheAndLogsSystemEvent()
    {
        var variantId = VariantId.NewId();
        var evt = new StockReservedEvent(InventoryId.NewId(), variantId, 3, 7);
        var notification = new DomainEventNotification<StockReservedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateInventoryCacheAsync(variantId, Arg.Any<CancellationToken>());
        await _audit.Received(1).LogSystemEventAsync(
            "CacheEvent",
            Arg.Is<string>(s => s!.Contains(variantId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StockReleasedEvent_InvalidatesInventoryCacheAndLogsSystemEvent()
    {
        var variantId = VariantId.NewId();
        var evt = new StockReleasedEvent(variantId, ProductId.NewId(), 2);
        var notification = new DomainEventNotification<StockReleasedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateInventoryCacheAsync(variantId, Arg.Any<CancellationToken>());
        await _audit.Received(1).LogSystemEventAsync(
            "CacheEvent",
            Arg.Is<string>(s => s!.Contains(variantId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StockCommittedEvent_InvalidatesInventoryCacheAndLogsSystemEvent()
    {
        var variantId = VariantId.NewId();
        var evt = new StockCommittedEvent(InventoryId.NewId(), variantId, OrderItemId.NewId(), 4);
        var notification = new DomainEventNotification<StockCommittedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateInventoryCacheAsync(variantId, Arg.Any<CancellationToken>());
        await _audit.Received(1).LogSystemEventAsync(
            "CacheEvent",
            Arg.Is<string>(s => s!.Contains(variantId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StockAdjustedEvent_InvalidatesInventoryCacheAndLogsSystemEvent()
    {
        var variantId = VariantId.NewId();
        var evt = new StockAdjustedEvent(InventoryId.NewId(), variantId, 12, 5, "restock");
        var notification = new DomainEventNotification<StockAdjustedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateInventoryCacheAsync(variantId, Arg.Any<CancellationToken>());
        await _audit.Received(1).LogSystemEventAsync(
            "CacheEvent",
            Arg.Is<string>(s => s!.Contains(variantId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StockReturnedEvent_InvalidatesInventoryCacheAndLogsSystemEvent()
    {
        var variantId = VariantId.NewId();
        var evt = new StockReturnedEvent(variantId, OrderId.NewId(), 1);
        var notification = new DomainEventNotification<StockReturnedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateInventoryCacheAsync(variantId, Arg.Any<CancellationToken>());
        await _audit.Received(1).LogSystemEventAsync(
            "CacheEvent",
            Arg.Is<string>(s => s!.Contains(variantId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }
}
