using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

/// <summary>
/// Returns the money of a refunded payment to the customer's wallet.
///
/// Only a payment that was funded from the wallet is given back: a gateway
/// payment never touched the wallet, so there is nothing to return. The key is the
/// same one the order cancellation uses, so an order that is cancelled and then
/// refunded is only ever paid back once.
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
            if (!domainEvent.Gateway.IsWallet)
            {
                await auditService.LogSystemEventAsync(
                    "WalletPaymentRefunded",
                    $"بازگشت وجه تراکنش {domainEvent.PaymentTransactionId.Value} از طریق درگاه پرداخت انجام شد و موجودی کیف پول تغییری نکرد.");
                return;
            }

            var command = new CreditWalletCommand(
                domainEvent.UserId.Value,
                domainEvent.Amount.Amount,
                WalletTransactionType.Refund,
                WalletReferenceType.Order,
                domainEvent.OrderId.Value.ToString(),
                $"refund-order-{domainEvent.OrderId.Value}",
                Description: "بازگشت وجه پرداخت کیف پول");

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "WalletRefundFailed",
                    $"بازگشت وجه کیف پول برای تراکنش {domainEvent.PaymentTransactionId.Value} ناموفق بود: {result.Error}. مبلغ {domainEvent.Amount.Amount} برای مغایرت نیازمند بررسی دستی است.");
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
