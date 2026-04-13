using Domain.Payment.Events;

namespace Application.Payment.EventHandlers;

public class PaymentSucceededNotificationEventHandler(
    INotificationService notificationService) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        try
        {
            await notificationService.CreateNotificationAsync(
                notification.UserId,
                "پرداخت موفق",
                $"پرداخت سفارش شما با موفقیت انجام شد. کد پیگیری: {notification.RefId}",
                "PaymentSuccess",
                ct: ct);
        }
        catch
        {
        }
    }
}