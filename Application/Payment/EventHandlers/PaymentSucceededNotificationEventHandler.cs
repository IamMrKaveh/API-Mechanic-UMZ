using Application.Notification.Contracts;
using Domain.Payment.Events;
using Domain.User.ValueObjects;

namespace Application.Payment.EventHandlers;

public class PaymentSucceededNotificationEventHandler(
    INotificationService notificationService,
    ILogger<PaymentSucceededNotificationEventHandler> logger) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        try
        {
            if (notification.UserId == 0) return;

            await notificationService.CreateNotificationAsync(
                UserId.From(Guid.Empty),
                "پرداخت موفق",
                $"پرداخت سفارش شما با موفقیت انجام شد. کد پیگیری: {notification.RefId}",
                "PaymentSuccess",
                ct: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send payment success notification for order {OrderId}",
                notification.OrderId);
        }
    }
}