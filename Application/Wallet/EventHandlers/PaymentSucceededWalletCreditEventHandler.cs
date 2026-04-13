using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class PaymentSucceededWalletCreditEventHandler(
    IMediator mediator,
    IAuditService auditService) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OrderId != 0)
            return;

        try
        {
            var command = new Application.Wallet.Features.Commands.CreditWallet.CreditWalletCommand(
                UserId: notification.UserId,
                Amount: notification.Amount,
                TransactionType: WalletTransactionType.TopUp,
                ReferenceType: WalletReferenceType.Payment,
                ReferenceId: notification.TransactionId.ToString(),
                IdempotencyKey: $"payment-topup-{notification.TransactionId}",
                Description: "شارژ حساب از طریق درگاه پرداخت"
            );

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                await auditService.LogSystemEventAsync(
                    "WalletTopUpFailed",
                    $"شارژ کیف پول برای تراکنش {notification.TransactionId} ناموفق بود: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentSucceededHandlerError",
                $"خطا در پردازش شارژ کیف پول برای تراکنش {notification.TransactionId}: {ex.Message}");
        }
    }
}