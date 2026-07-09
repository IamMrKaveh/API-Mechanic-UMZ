using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class SendWalletCreditNotificationHandler(
    INotificationService notificationService,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletCreditedEvent>>
{
    private const string AdminAdjustmentDescriptionPrefix = "[ADMIN-CREDIT]";
    private const string NotificationType = "WalletCredit";

    public async Task Handle(
        DomainEventNotification<WalletCreditedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        if (string.IsNullOrWhiteSpace(evt.Description)
            || !evt.Description.StartsWith(AdminAdjustmentDescriptionPrefix, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            var title = "شارژ کیف پول توسط پشتیبانی";
            var message =
                $"مبلغ {evt.Amount.Amount:N0} {evt.Amount.Currency} توسط پشتیبانی به کیف پول شما افزوده شد. " +
                $"موجودی جدید: {evt.NewBalance.Amount:N0} {evt.NewBalance.Currency}.";

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
                "WalletCreditNotificationFailed",
                $"ارسال نوتیفیکیشن شارژ ادمین برای کاربر {evt.OwnerId.Value} ناموفق بود: {ex.Message}",
                ct);
        }
    }
}