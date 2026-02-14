namespace Application.Order.EventHandlers;

/// <summary>
/// وقتی سفارش ثبت شد، نوتیفیکیشن ارسال می‌شود
/// </summary>
public sealed class OrderPlacedNotificationEventHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderPlacedNotificationEventHandler> _logger;

    public OrderPlacedNotificationEventHandler(
        INotificationService notificationService,
        ILogger<OrderPlacedNotificationEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateNotificationAsync(
                notification.UserId,
                "سفارش ثبت شد",
                $"سفارش شماره {notification.OrderNumber} با موفقیت ثبت شد.",
                "OrderCreated",
                $"/dashboard/orders/{notification.OrderId}",
                notification.OrderId,
                "Order",
                cancellationToken);

            _logger.LogInformation("Order placed notification sent for order {OrderId}", notification.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle OrderPlacedEvent for order {OrderId}", notification.OrderId);
        }
    }
}