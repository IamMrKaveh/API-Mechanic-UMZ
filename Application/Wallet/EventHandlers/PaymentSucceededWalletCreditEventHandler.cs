using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

/// <summary>
/// هنگام موفقیت پرداخت (top-up)، مستقیماً کیف پول کاربر را شارژ می‌کند.
/// فقط برای پرداخت‌هایی که OrderId آنها صفر است (شارژ کیف پول بدون سفارش) اجرا می‌شود.
/// برای خریدهای عادی این هندلر کاری انجام نمی‌دهد.
/// </summary>
public class PaymentSucceededWalletCreditEventHandler(
    IMediator mediator,
    ILogger<PaymentSucceededWalletCreditEventHandler> logger) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OrderId != 0)
            return;

        try
        {
            logger.LogInformation(
                "[WalletCredit] PaymentSucceeded (top-up): TransactionId={TxId}, UserId={UserId}, Amount={Amount}",
                notification.TransactionId, notification.UserId, notification.Amount);

            var command = new Application.Wallet.Features.Commands.CreditWallet.CreditWalletCommand(
                UserId: notification.UserId,
                Amount: notification.Amount,
                TransactionType: WalletTransactionType.TopUp,
                ReferenceType: WalletReferenceType.Payment,
                ReferenceId: notification.TransactionId,
                IdempotencyKey: $"payment-topup-{notification.TransactionId}",
                Description: "شارژ حساب از طریق درگاه پرداخت"
            );

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailure)
            {
                logger.LogError(
                    "[WalletCredit] CreditWalletCommand failed for TransactionId={TxId}: {Error}",
                    notification.TransactionId, result.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error handling PaymentSucceededEvent for wallet credit, TransactionId={TxId}",
                notification.TransactionId);
        }
    }
}