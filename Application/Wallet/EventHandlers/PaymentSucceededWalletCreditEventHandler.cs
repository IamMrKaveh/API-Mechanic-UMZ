using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class PaymentSucceededWalletCreditEventHandler(
    IMediator mediator,
    IAuditService auditService) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OrderId is null)
            return;

        try
        {
            var command = new CreditWalletCommand(
                UserId: notification.UserId.Value,
                Amount: notification.Amount.Amount,
                TransactionType: WalletTransactionType.Credit,
                ReferenceType: WalletReferenceType.Payment,
                ReferenceId: notification.PaymentTransactionId.ToString(),
                IdempotencyKey: $"payment-topup-{notification.PaymentTransactionId}",
                Description: "شارژ حساب از طریق درگاه پرداخت"
            );

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "WalletTopUpFailed",
                    $"شارژ کیف پول برای تراکنش {notification.PaymentTransactionId} ناموفق بود: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentSucceededHandlerError",
                $"خطا در پردازش شارژ کیف پول برای تراکنش {notification.PaymentTransactionId}: {ex.Message}");
        }
    }
}