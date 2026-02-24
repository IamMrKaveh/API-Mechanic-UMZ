namespace Infrastructure.Cache.EventHandlers;

public sealed class OrderCacheInvalidationHandler :
    INotificationHandler<OrderCreatedEvent>,
    INotificationHandler<OrderPaidEvent>,
    INotificationHandler<OrderCancelledEvent>,
    INotificationHandler<OrderStatusChangedEvent>
{
    private readonly ICacheInvalidationService _invalidation;

    public OrderCacheInvalidationHandler(ICacheInvalidationService invalidation)
        => _invalidation = invalidation;

    public Task Handle(OrderCreatedEvent n, CancellationToken ct) => Invalidate(n.UserId, ct);

    public Task Handle(OrderPaidEvent n, CancellationToken ct) => Invalidate(n.UserId, ct);

    public Task Handle(OrderCancelledEvent n, CancellationToken ct) => _invalidation.InvalidateByPatternAsync($"order:{n.OrderId}", ct);

    public Task Handle(OrderStatusChangedEvent n, CancellationToken ct) => _invalidation.InvalidateByPatternAsync($"order:{n.OrderId}", ct);

    private Task Invalidate(int userId, CancellationToken ct) =>
        _invalidation.InvalidateUserOrdersAsync(userId, ct);
}