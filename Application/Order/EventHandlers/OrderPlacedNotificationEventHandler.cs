using Domain.Order.Events;

namespace Application.Order.EventHandlers;

/// <summary>
/// وقتی سفارش ثبت شد، نوتیفیکیشن ارسال می‌شود
/// </summary>
public sealed class OrderPlacedNotificationEventHandler(
    INotificationService notificationService,
    ILogger<OrderPlacedNotificationEventHandler> logger) : INotificationHandler<OrderCreatedEvent>
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
                $"/dashboard/orders/{notification.OrderId}",
                notification.OrderId,
                "Order",
                ct);

            logger.LogInformation("Order placed notification sent for order {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle OrderPlacedEvent for order {OrderId}", notification.OrderId);
        }
    }
}