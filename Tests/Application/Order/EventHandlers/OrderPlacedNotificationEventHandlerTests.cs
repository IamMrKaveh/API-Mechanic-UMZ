using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Order.EventHandlers;
using Domain.Order.Events;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Application.Order.EventHandlers;

public class OrderPlacedNotificationEventHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly OrderPlacedNotificationEventHandler _sut;

    public OrderPlacedNotificationEventHandlerTests()
    {
        _sut = new OrderPlacedNotificationEventHandler(_notificationService);
    }

    private static OrderCreatedEvent BuildEvent(
        OrderId? orderId = null,
        UserId? userId = null,
        OrderNumber? orderNumber = null,
        decimal finalAmount = 250_000m,
        string currency = "IRT",
        int itemsCount = 3,
        Guid? idempotencyKey = null)
    {
        return new OrderCreatedEvent(
            orderId ?? OrderId.NewId(),
            userId ?? UserId.NewId(),
            orderNumber ?? OrderNumber.Generate(new DateOnly(2026, 8, 30)),
            finalAmount,
            currency,
            itemsCount,
            idempotencyKey ?? Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WithValidEvent_InvokesNotificationServiceOnce()
    {
        var evt = BuildEvent();
        var notification = new DomainEventNotification<OrderCreatedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidEvent_ForwardsUserIdOrderIdAndTypeToNotificationService()
    {
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
        var orderNumber = OrderNumber.Generate(new DateOnly(2026, 8, 30));
        var evt = BuildEvent(orderId, userId, orderNumber);
        var notification = new DomainEventNotification<OrderCreatedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            userId,
            Arg.Any<string>(),
            Arg.Is<string>(m => m!.Contains(orderNumber.ToString())),
            "OrderCreated",
            Arg.Is<string>(l => l!.Contains(orderId.Value.ToString())),
            orderId.Value,
            "Order",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_DoesNotPropagateException()
    {
        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("downstream failure"));

        var notification = new DomainEventNotification<OrderCreatedEvent>(BuildEvent());

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToNotificationService()
    {
        using var cts = new CancellationTokenSource();
        var notification = new DomainEventNotification<OrderCreatedEvent>(BuildEvent());

        await _sut.Handle(notification, cts.Token);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            cts.Token);
    }
}
