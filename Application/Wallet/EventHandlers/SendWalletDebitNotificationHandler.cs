using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class SendWalletDebitNotificationHandler(
    INotificationService notificationService,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletDebitedEvent>>
{
    private const string AdminAdjustmentDescriptionPrefix = "[ADMIN-DEBIT]";
    private const string NotificationType = "WalletDebit";

    public async Task Handle(
        DomainEventNotification<WalletDebitedEvent> notification,
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
            var title = "کسر از کیف پول توسط پشتیبانی";
            var message =
                $"مبلغ {evt.Amount.Amount:N0} {evt.Amount.Currency} توسط پشتیبانی از کیف پول شما کسر شد. " +
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
                "WalletDebitNotificationFailed",
                $"ارسال نوتیفیکیشن کسر ادمین برای کاربر {evt.OwnerId.Value} ناموفق بود: {ex.Message}",
                ct);
        }
    }
}