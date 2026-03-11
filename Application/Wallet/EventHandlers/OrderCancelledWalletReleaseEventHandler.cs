using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class OrderCancelledWalletReleaseEventHandler(
    IMediator mediator,
    IWalletQueryService walletQueryService,
    ILogger<OrderCancelledWalletReleaseEventHandler> logger) : INotificationHandler<OrderCancelledEvent>
{
    private readonly IMediator _mediator = mediator;
    private readonly IWalletQueryService _walletQueryService = walletQueryService;
    private readonly ILogger<OrderCancelledWalletReleaseEventHandler> _logger = logger;

    public async Task Handle(
        OrderCancelledEvent notification,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "[WalletRelease] OrderCancelled: OrderId={OrderId}, UserId={UserId}",
                notification.OrderId, notification.UserId);

            var paymentEntry = await _walletQueryService.GetOrderPaymentLedgerEntryAsync(
                notification.UserId, notification.OrderId, ct);

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

                var result = await _mediator.Send(refundCommand, ct);
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

                await _mediator.Send(releaseCommand, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling wallet for cancelled order {OrderId}", notification.OrderId);
        }
    }
}