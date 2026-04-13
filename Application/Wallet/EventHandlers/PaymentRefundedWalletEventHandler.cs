using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class PaymentRefundedWalletEventHandler(
    IMediator mediator,
    IAuditService auditService) : INotificationHandler<PaymentRefundedEvent>
{
    public async Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreditWalletCommand(
                notification.UserId.Value,
                notification.Amount.Amount,
                WalletTransactionType.Refund,
                WalletReferenceType.Payment,
                notification.PaymentTransactionId.Value.ToString(),
                $"refund-payment-{notification.PaymentTransactionId.Value}",
                Description: "استرداد وجه به کیف پول"
            );

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "WalletRefundFailed",
                    $"شارژ کیف پول برای استرداد تراکنش {notification.PaymentTransactionId.Value} ناموفق بود: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentRefundedHandlerError",
                $"خطا در پردازش استرداد پرداخت {notification.PaymentTransactionId.Value}: {ex.Message}");
        }
    }
}