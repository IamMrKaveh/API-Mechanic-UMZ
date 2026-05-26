using Domain.Order.Events;

namespace Application.Order.EventHandlers;

public sealed class OrderPlacedNotificationEventHandler(
    INotificationService notificationService) : INotificationHandler<DomainEventNotification<OrderCreatedEvent>>
{
    public async Task Handle(DomainEventNotification<OrderCreatedEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        try
        {
            await notificationService.CreateNotificationAsync(
                domainEvent.UserId,
                "سفارش ثبت شد",
                $"سفارش شماره {domainEvent.OrderNumber} با موفقیت ثبت شد.",
                "OrderCreated",
                $"/dashboard/orders/{domainEvent.OrderId.Value}",
                domainEvent.OrderId.Value,
                "Order",
                ct);
        }
        catch
        {
        }
    }
}