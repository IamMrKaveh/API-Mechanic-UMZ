using Application.Notification.Contracts;
using Domain.Payment.Events;

namespace Application.Payment.EventHandlers;

public class PaymentSucceededNotificationEventHandler(
    INotificationService notificationService,
    ILogger<PaymentSucceededNotificationEventHandler> logger) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        try
        {
            if (notification.UserId is null)
                return;

            if (notification is null)
                return;

            await notificationService.CreateNotificationAsync(
                notification.UserId,
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