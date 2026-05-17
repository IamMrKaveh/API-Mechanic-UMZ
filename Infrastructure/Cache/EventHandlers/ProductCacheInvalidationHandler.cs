using Domain.Product.Events;

namespace Infrastructure.Cache.EventHandlers;

public sealed class ProductCacheInvalidationHandler(ICacheInvalidationService invalidation) :
    INotificationHandler<DomainEventNotification<ProductUpdatedEvent>>,
    INotificationHandler<DomainEventNotification<PriceChangedEvent>>,
    INotificationHandler<DomainEventNotification<ProductActivatedEvent>>,
    INotificationHandler<DomainEventNotification<ProductDeactivatedEvent>>
{
    public Task Handle(DomainEventNotification<ProductUpdatedEvent> n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.DomainEvent.ProductId, ct);

    public Task Handle(DomainEventNotification<PriceChangedEvent> n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.DomainEvent.ProductId, ct);

    public Task Handle(DomainEventNotification<ProductActivatedEvent> n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.DomainEvent.ProductId, ct);

    public Task Handle(DomainEventNotification<ProductDeactivatedEvent> n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.DomainEvent.ProductId, ct);
}