namespace Infrastructure.Cache.EventHandlers;

public sealed class ProductCacheInvalidationHandler :
    INotificationHandler<ProductUpdatedEvent>,
    INotificationHandler<PriceChangedEvent>,
    INotificationHandler<ProductActivatedEvent>,
    INotificationHandler<ProductDeactivatedEvent>
{
    private readonly ICacheInvalidationService _invalidation;

    public ProductCacheInvalidationHandler(ICacheInvalidationService invalidation)
        => _invalidation = invalidation;

    public Task Handle(ProductUpdatedEvent n, CancellationToken ct) => _invalidation.InvalidateProductAsync(n.ProductId, ct);

    public Task Handle(PriceChangedEvent n, CancellationToken ct) => _invalidation.InvalidateProductAsync(n.ProductId, ct);

    public Task Handle(ProductActivatedEvent n, CancellationToken ct) => _invalidation.InvalidateProductAsync(n.ProductId, ct);

    public Task Handle(ProductDeactivatedEvent n, CancellationToken ct) => _invalidation.InvalidateProductAsync(n.ProductId, ct);
}