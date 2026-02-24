namespace Application.Wallet.EventHandlers;

/// <summary>
/// هنگام استرداد وجه، مستقیماً کیف پول کاربر را شارژ می‌کند.
/// </summary>
public class PaymentRefundedWalletEventHandler : INotificationHandler<PaymentRefundedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentRefundedWalletEventHandler> _logger;

    public PaymentRefundedWalletEventHandler(
        IMediator mediator,
        ILogger<PaymentRefundedWalletEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[WalletRefund] PaymentRefunded: TransactionId={TxId}, OrderId={OrderId}, UserId={UserId}, Amount={Amount}",
                notification.TransactionId, notification.OrderId, notification.UserId, notification.Amount);

            var command = new CreditWalletCommand(
                UserId: notification.UserId,
                Amount: notification.Amount,
                TransactionType: WalletTransactionType.Refund,
                ReferenceType: WalletReferenceType.Payment,
                ReferenceId: notification.TransactionId,
                IdempotencyKey: $"refund-payment-{notification.TransactionId}",
                Description: "استرداد وجه به کیف پول"
            );

            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsFailed)
            {
                _logger.LogError(
                    "[WalletRefund] CreditWalletCommand failed for TransactionId={TxId}: {Error}",
                    notification.TransactionId, result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling PaymentRefundedEvent for wallet, TransactionId={TxId}",
                notification.TransactionId);
        }
    }
}