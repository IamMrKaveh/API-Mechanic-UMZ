using Domain.Payment.Events;

namespace Infrastructure.Payment.EventHandlers;

public class PaymentSucceededNotificationEventHandler(
    INotificationService notificationService) : INotificationHandler<DomainEventNotification<PaymentSucceededEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentSucceededEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        try
        {
            await notificationService.CreateNotificationAsync(
                domainEvent.UserId,
                "پرداخت موفق",
                $"پرداخت سفارش شما با موفقیت انجام شد. کد پیگیری: {domainEvent.RefId}",
                "PaymentSuccess",
                ct: ct);
        }
        catch
        {
        }
    }
}