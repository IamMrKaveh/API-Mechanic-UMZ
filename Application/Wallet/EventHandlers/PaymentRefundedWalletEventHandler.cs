using Application.Common.Events;
using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class PaymentRefundedWalletEventHandler(
    IMediator mediator,
    IAuditService auditService) : INotificationHandler<DomainEventNotification<PaymentRefundedEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentRefundedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        try
        {
            var command = new CreditWalletCommand(
                domainEvent.UserId.Value,
                domainEvent.Amount.Amount,
                WalletTransactionType.Refund,
                WalletReferenceType.Payment,
                domainEvent.PaymentTransactionId.Value.ToString(),
                $"refund-payment-{domainEvent.PaymentTransactionId.Value}",
                Description: "استرداد وجه به کیف پول"
            );

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "WalletRefundFailed",
                    $"شارژ کیف پول برای استرداد تراکنش {domainEvent.PaymentTransactionId.Value} ناموفق بود: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentRefundedHandlerError",
                $"خطا در پردازش استرداد پرداخت {domainEvent.PaymentTransactionId.Value}: {ex.Message}");
        }
    }
}