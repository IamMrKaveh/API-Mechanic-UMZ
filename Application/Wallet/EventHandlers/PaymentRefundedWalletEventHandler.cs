namespace Application.Wallet.EventHandlers;

/// <summary>
/// When a payment is refunded, credit the user's wallet.
/// </summary>
public class PaymentRefundedWalletEventHandler : INotificationHandler<PaymentRefundedEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentRefundedWalletEventHandler> _logger;

    public PaymentRefundedWalletEventHandler(IMediator mediator, ILogger<PaymentRefundedWalletEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(PaymentRefundedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[WalletRefund] PaymentRefunded: TransactionId={TxId}, OrderId={OrderId}, Amount={Amount}",
                notification.TransactionId, notification.OrderId, notification.Amount);

            // We need the userId – enrich from the repository inside the command handler
            await _mediator.Publish(
                new WalletRefundApplicationEvent(notification.TransactionId, notification.OrderId, notification.Amount),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PaymentRefundedEvent for wallet, TransactionId={TxId}", notification.TransactionId);
        }
    }
}