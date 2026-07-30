using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class SendWalletDebitRequestCreatedNotificationHandler(
    INotificationService notificationService,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletDebitRequestCreatedEvent>>
{
    private const string NotificationType = "WalletDebitRequest";

    public async Task Handle(
        DomainEventNotification<WalletDebitRequestCreatedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        try
        {
            var title = "درخواست کسر از کیف پول";
            var message =
                $"ادمین درخواست کسر مبلغ {evt.Amount.Amount:N0} {evt.Amount.Currency} از کیف پول شما را ثبت کرده است. " +
                $"لطفاً در داشبورد خود تایید یا رد کنید.";

            await notificationService.CreateNotificationAsync(
                evt.OwnerId,
                title,
                message,
                NotificationType,
                actionUrl: "/wallet",
                referenceId: evt.WalletId.Value,
                referenceType: "Wallet",
                ct: ct);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletDebitRequestCreatedNotificationFailed",
                $"ارسال نوتیفیکیشن درخواست کسر برای کاربر {evt.OwnerId.Value} ناموفق بود: {ex.Message}",
                ct);
        }
    }
}
