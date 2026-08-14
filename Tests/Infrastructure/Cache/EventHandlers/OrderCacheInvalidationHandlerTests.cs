using Application.Cache.Contracts;
using Application.Common.Events;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Cache.EventHandlers;

namespace Tests.Infrastructure.Cache.EventHandlers;

public class OrderCacheInvalidationHandlerTests
{
    private readonly ICacheInvalidationService _invalidation = Substitute.For<ICacheInvalidationService>(); private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly OrderCacheInvalidationHandler _sut;

    public OrderCacheInvalidationHandlerTests()
    {
        _sut = new OrderCacheInvalidationHandler(_invalidation, _cache);
    }

    private static OrderNumber SampleOrderNumber() =>
        OrderNumber.Generate(DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public async Task Handle_OrderCreatedEvent_InvalidatesUserCacheAndUserOrdersPrefix()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var evt = new OrderCreatedEvent(orderId, userId, SampleOrderNumber(), 100m, "IRR", 2, Guid.NewGuid());
        var notification = new DomainEventNotification<OrderCreatedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateUserCacheAsync(userId, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByPrefixAsync($"orders:user:{userId.Value}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderPaidEvent_InvalidatesUserCacheAndUserOrdersPrefix()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var evt = new OrderPaidEvent(
            orderId,
            SampleOrderNumber(),
            userId,
            PaymentTransactionId.NewId(),
            50m,
            "IRR");
        var notification = new DomainEventNotification<OrderPaidEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateUserCacheAsync(userId, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByPrefixAsync($"orders:user:{userId.Value}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderCancelledEvent_RemovesOrderCacheEntriesByPrefixAndDoesNotTouchInvalidationService()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var evt = new OrderCancelledEvent(orderId, SampleOrderNumber(), userId, "user-request", wasPaid: false);
        var notification = new DomainEventNotification<OrderCancelledEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _cache.Received(1).RemoveByPrefixAsync($"order:{orderId}", Arg.Any<CancellationToken>());
        await _invalidation.DidNotReceiveWithAnyArgs().InvalidateUserCacheAsync(default!, default);
    }

    [Fact]
    public async Task Handle_OrderStatusChangedEvent_RemovesOrderCacheEntriesByPrefixAndDoesNotTouchInvalidationService()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var evt = new OrderStatusChangedEvent(
            orderId,
            SampleOrderNumber(),
            userId,
            OrderStatusValue.Created,
            OrderStatusValue.Paid);
        var notification = new DomainEventNotification<OrderStatusChangedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _cache.Received(1).RemoveByPrefixAsync($"order:{orderId}", Arg.Any<CancellationToken>());
        await _invalidation.DidNotReceiveWithAnyArgs().InvalidateUserCacheAsync(default!, default);
    }
}
