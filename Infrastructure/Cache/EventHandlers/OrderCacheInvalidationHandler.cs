using Domain.Order.Events;
using Domain.User.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

public sealed class OrderCacheInvalidationHandler(
    ICacheInvalidationService invalidation,
    ICacheService cacheService) :
    INotificationHandler<DomainEventNotification<OrderCreatedEvent>>,
    INotificationHandler<DomainEventNotification<OrderPaidEvent>>,
    INotificationHandler<DomainEventNotification<OrderCancelledEvent>>,
    INotificationHandler<DomainEventNotification<OrderStatusChangedEvent>>
{
    public Task Handle(DomainEventNotification<OrderCreatedEvent> n, CancellationToken ct)
        => InvalidateUserOrders(n.DomainEvent.UserId, ct);

    public Task Handle(DomainEventNotification<OrderPaidEvent> n, CancellationToken ct)
        => InvalidateUserOrders(n.DomainEvent.UserId, ct);

    public Task Handle(DomainEventNotification<OrderCancelledEvent> n, CancellationToken ct)
        => cacheService.RemoveByPrefixAsync($"order:{n.DomainEvent.OrderId}", ct);

    public Task Handle(DomainEventNotification<OrderStatusChangedEvent> n, CancellationToken ct)
        => cacheService.RemoveByPrefixAsync($"order:{n.DomainEvent.OrderId}", ct);

    private async Task InvalidateUserOrders(UserId userId, CancellationToken ct)
    {
        await invalidation.InvalidateUserCacheAsync(userId, ct);
        await cacheService.RemoveByPrefixAsync($"orders:user:{userId.Value}", ct);
    }
}