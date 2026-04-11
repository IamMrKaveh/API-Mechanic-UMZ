using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

/// <summary>
/// هنگام استرداد وجه، مستقیماً کیف پول کاربر را شارژ می‌کند.
/// </summary>
public class PaymentRefundedWalletEventHandler(
    IMediator mediator,
    ILogger<PaymentRefundedWalletEventHandler> logger) : INotificationHandler<PaymentRefundedEvent>
{
    public async Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "[WalletRefund] PaymentRefunded: TransactionId={TxId}, OrderId={OrderId}, UserId={UserId}, Amount={Amount}",
                notification.TransactionId, notification.OrderId, notification.UserId, notification.Amount);

            var command = new CreditWalletCommand(
                notification.UserId,
                notification.Amount,
                WalletTransactionType.Refund,
                WalletReferenceType.Payment,
                notification.TransactionId,
                $"refund-payment-{notification.TransactionId}",
                "استرداد وجه به کیف پول"
            );

            var result = await mediator.Send(command, cancellationToken);
            if (result.IsFailed)
            {
                logger.LogError(
                    "[WalletRefund] CreditWalletCommand failed for TransactionId={TxId}: {Error}",
                    notification.TransactionId, result.Error);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error handling PaymentRefundedEvent for wallet, TransactionId={TxId}",
                notification.TransactionId);
        }
    }
}