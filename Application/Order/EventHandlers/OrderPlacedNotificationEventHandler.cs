using Domain.Order.Events;

namespace Application.Order.EventHandlers;

public sealed class OrderPlacedNotificationEventHandler(
    INotificationService notificationService) : INotificationHandler<OrderCreatedEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        try
        {
            await notificationService.CreateNotificationAsync(
                notification.UserId,
                "سفارش ثبت شد",
                $"سفارش شماره {notification.OrderNumber} با موفقیت ثبت شد.",
                "OrderCreated",
                $"/dashboard/orders/{notification.OrderId.Value}",
                notification.OrderId.Value,
                "Order",
                ct);
        }
        catch
        {
        }
    }
}