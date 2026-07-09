using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class SendWalletUnfreezeNotificationHandler(
    INotificationService notificationService,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletUnfrozenEvent>>
{
    private const string NotificationType = "WalletUnfrozen";

    public async Task Handle(
        DomainEventNotification<WalletUnfrozenEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        try
        {
            var title = "رفع مسدودی کیف پول";
            var message =
                "کیف پول شما توسط پشتیبانی رفع مسدود شد و اکنون قابل استفاده است.";

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
                "WalletUnfreezeNotificationFailed",
                $"ارسال نوتیفیکیشن رفع مسدودی کیف پول برای کاربر {evt.OwnerId.Value} ناموفق بود: {ex.Message}",
                ct);
        }
    }
}