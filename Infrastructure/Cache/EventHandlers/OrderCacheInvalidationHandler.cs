using Application.Cache.Contracts;
using Domain.Order.Events;
using Domain.User.ValueObjects;

namespace Infrastructure.Cache.EventHandlers;

public sealed class OrderCacheInvalidationHandler(
    ICacheInvalidationService invalidation,
    ICacheService cacheService) :
    INotificationHandler<OrderCreatedEvent>,
    INotificationHandler<OrderPaidEvent>,
    INotificationHandler<OrderCancelledEvent>,
    INotificationHandler<OrderStatusChangedEvent>
{
    public Task Handle(OrderCreatedEvent n, CancellationToken ct)
        => InvalidateUserOrders(n.UserId, ct);

    public Task Handle(OrderPaidEvent n, CancellationToken ct)
        => InvalidateUserOrders(n.UserId, ct);

    public Task Handle(OrderCancelledEvent n, CancellationToken ct)
        => cacheService.RemoveByPrefixAsync($"order:{n.OrderId}", ct);

    public Task Handle(OrderStatusChangedEvent n, CancellationToken ct)
        => cacheService.RemoveByPrefixAsync($"order:{n.OrderId}", ct);

    private async Task InvalidateUserOrders(UserId userId, CancellationToken ct)
    {
        await invalidation.InvalidateUserCacheAsync(userId, ct);
        await cacheService.RemoveByPrefixAsync($"orders:user:{userId.Value}", ct);
    }
}