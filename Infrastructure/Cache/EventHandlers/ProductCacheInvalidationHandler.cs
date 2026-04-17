using Application.Cache.Contracts;
using Domain.Product.Events;
using Domain.Product.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

public sealed class ProductCacheInvalidationHandler(ICacheInvalidationService invalidation) :
    INotificationHandler<ProductUpdatedEvent>,
    INotificationHandler<PriceChangedEvent>,
    INotificationHandler<ProductActivatedEvent>,
    INotificationHandler<ProductDeactivatedEvent>
{
    public Task Handle(ProductUpdatedEvent n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.ProductId, ct);

    public Task Handle(PriceChangedEvent n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.ProductId, ct);

    public Task Handle(ProductActivatedEvent n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.ProductId, ct);

    public Task Handle(ProductDeactivatedEvent n, CancellationToken ct)
        => invalidation.InvalidateProductCacheAsync(n.ProductId, ct);
}