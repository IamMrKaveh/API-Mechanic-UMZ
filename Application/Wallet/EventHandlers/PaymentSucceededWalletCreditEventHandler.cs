namespace Application.Wallet.EventHandlers;

/// <summary>
/// When a real-money payment succeeds, top up the wallet.
/// Only applies to payments made TO the wallet (gateway = Wallet top-up).
/// </summary>
public class PaymentSucceededWalletCreditEventHandler : INotificationHandler<PaymentSucceededEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentSucceededWalletCreditEventHandler> _logger;

    public PaymentSucceededWalletCreditEventHandler(IMediator mediator, ILogger<PaymentSucceededWalletCreditEventHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Handle(PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[WalletCredit] PaymentSucceeded: TransactionId={TxId}, UserId={UserId}, OrderId={OrderId}",
                notification.TransactionId, notification.UserId, notification.OrderId);

            await _mediator.Publish(
                new WalletTopUpApplicationEvent(
                    notification.UserId,
                    notification.TransactionId,
                    notification.OrderId),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PaymentSucceededEvent for wallet credit, TransactionId={TxId}", notification.TransactionId);
        }
    }
}