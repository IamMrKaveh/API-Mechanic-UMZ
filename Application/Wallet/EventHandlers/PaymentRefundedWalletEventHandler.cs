using Application.Wallet.Features.Commands.DebitWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

/// <summary>
/// Reverses the wallet credit that a successful gateway payment granted.
///
/// A payment made from the wallet is not touched here: the money was returned
/// by the order cancellation path, which derives the exact amount from the
/// wallet ledger and is idempotent per order. A payment made through a gateway
/// did grant a wallet credit, so the refund has to take that credit back.
/// </summary>
public class PaymentRefundedWalletEventHandler(
    IMediator mediator,
    IAuditService auditService) : INotificationHandler<DomainEventNotification<PaymentRefundedEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentRefundedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        try
        {
            if (domainEvent.Gateway.IsWallet)
            {
                await auditService.LogSystemEventAsync(
                    "WalletPaymentRefunded",
                    $"بازگشت وجه تراکنش {domainEvent.PaymentTransactionId.Value} از طریق کیف پول انجام شد و موجودی کیف پول در این مسیر تغییر نمی‌کند.");
                return;
            }

            var command = new DebitWalletCommand(
                domainEvent.UserId.Value,
                domainEvent.Amount.Amount,
                WalletTransactionType.Refund,
                WalletReferenceType.Payment,
                $"refund-payment-{domainEvent.PaymentTransactionId.Value}",
                Description: "برگشت اعتبار کیف پول از بازگشت وجه درگاه پرداخت");

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "WalletRefundFailed",
                    $"برگشت اعتبار کیف پول برای تراکنش {domainEvent.PaymentTransactionId.Value} ناموفق بود: {result.Error}. مبلغ {domainEvent.Amount.Amount} برای مغایرت نیازمند بررسی دستی است.");
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentRefundedHandlerError",
                $"خطا در ثبت رویداد بازگشت وجه تراکنش {domainEvent.PaymentTransactionId.Value}: {ex.Message}");
        }
    }
}
