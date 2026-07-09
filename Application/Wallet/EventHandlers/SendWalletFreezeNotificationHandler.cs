using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class SendWalletFreezeNotificationHandler(
    INotificationService notificationService,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletFrozenEvent>>
{
    private const string NotificationType = "WalletFrozen";

    public async Task Handle(
        DomainEventNotification<WalletFrozenEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        try
        {
            var title = "مسدود شدن کیف پول";
            var reason = string.IsNullOrWhiteSpace(evt.Reason) ? "بدون دلیل ثبت‌شده" : evt.Reason;
            var message =
                $"کیف پول شما توسط پشتیبانی مسدود شد. دلیل: {reason}. " +
                "برای پیگیری با پشتیبانی تماس بگیرید.";

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
                "WalletFreezeNotificationFailed",
                $"ارسال نوتیفیکیشن مسدودسازی کیف پول برای کاربر {evt.OwnerId.Value} ناموفق بود: {ex.Message}",
                ct);
        }
    }
}