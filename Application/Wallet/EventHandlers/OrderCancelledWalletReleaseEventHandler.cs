namespace Application.Wallet.EventHandlers;

/// <summary>
/// هنگام لغو سفارش:
/// - اگر از کیف پول کسر شده بود → استرداد (Refund) می‌کند.
/// - اگر فقط رزرو شده بود → رزرو را آزاد می‌کند.
/// </summary>
public class OrderCancelledWalletReleaseEventHandler : INotificationHandler<OrderCancelledEvent>
{
    private readonly IMediator _mediator;
    private readonly IWalletRepository _walletRepository;
    private readonly ILogger<OrderCancelledWalletReleaseEventHandler> _logger;

    public OrderCancelledWalletReleaseEventHandler(
        IMediator mediator,
        IWalletRepository walletRepository,
        ILogger<OrderCancelledWalletReleaseEventHandler> logger)
    {
        _mediator = mediator;
        _walletRepository = walletRepository;
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[WalletRelease] OrderCancelled: OrderId={OrderId}, UserId={UserId}",
                notification.OrderId, notification.UserId);

            var paymentEntry = await _walletRepository.GetOrderPaymentLedgerEntryAsync(
                notification.UserId, notification.OrderId, cancellationToken);

            if (paymentEntry != null)
            {
                _logger.LogInformation(
                    "[WalletRefund] Order {OrderId} was paid via Wallet; issuing refund of {Amount}.",
                    notification.OrderId, paymentEntry.AmountDelta);

                var refundCommand = new CreditWalletCommand(
                    UserId: notification.UserId,
                    Amount: paymentEntry.AmountDelta,
                    TransactionType: WalletTransactionType.Refund,
                    ReferenceType: WalletReferenceType.Order,
                    ReferenceId: notification.OrderId,
                    IdempotencyKey: $"refund-order-{notification.OrderId}",
                    Description: $"استرداد وجه سفارش لغو شده #{notification.OrderId}"
                );

                var result = await _mediator.Send(refundCommand, cancellationToken);
                if (result.IsFailed)
                {
                    _logger.LogError(
                        "[WalletRefund] Refund failed for Order {OrderId}: {Error}",
                        notification.OrderId, result.Error);
                }
            }
            else
            {
                var releaseCommand = new ReleaseWalletReservationCommand(
                    notification.UserId,
                    notification.OrderId);

                await _mediator.Send(releaseCommand, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling wallet for cancelled order {OrderId}", notification.OrderId);
        }
    }
}