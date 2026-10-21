using Domain.Payment.Events;

namespace Application.Wallet.EventHandlers;

/// <summary>
/// Records a gateway refund for reconciliation.
///
/// The wallet balance is intentionally not modified here. A cancelled order that was
/// paid from the wallet is refunded by
/// <see cref="OrderCancelledWalletReleaseEventHandler"/>, which derives the exact
/// amount from the wallet ledger and is idempotent per order. Crediting the wallet
/// again on the payment refund would refund the customer twice, and crediting a
/// gateway refund that was never debited would create balance out of nothing.
/// </summary>
public class PaymentRefundedWalletEventHandler(
    IAuditService auditService) : INotificationHandler<DomainEventNotification<PaymentRefundedEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentRefundedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentRefunded",
                $"بازگشت وجه تراکنش {domainEvent.PaymentTransactionId.Value} به مبلغ {domainEvent.Amount.Amount} ثبت شد. " +
                $"موجودی کیف پول در این مسیر تغییر نمی‌کند؛ بازپرداخت سفارش لغوشده از طریق دفتر کیف پول انجام می‌شود.");
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentRefundedHandlerError",
                $"خطا در ثبت رویداد بازگشت وجه تراکنش {domainEvent.PaymentTransactionId.Value}: {ex.Message}");
        }
    }
}
