using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.Order.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class OrderCancelledWalletReleaseEventHandler(
    IMediator mediator,
    IWalletQueryService walletQueryService,
    ILogger<OrderCancelledWalletReleaseEventHandler> logger) : INotificationHandler<OrderCancelledEvent>
{
    public async Task Handle(
        OrderCancelledEvent notification,
        CancellationToken ct)
    {
        try
        {
            logger.LogInformation(
                "[WalletRelease] OrderCancelled: OrderId={OrderId}, UserId={UserId}",
                notification.OrderId, notification.UserId);

            var paymentEntry = await walletQueryService.GetOrderPaymentLedgerEntryAsync(
                notification.UserId, notification.OrderId, ct);

            if (paymentEntry != null)
            {
                logger.LogInformation(
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

                var result = await mediator.Send(refundCommand, ct);
                if (result.IsFailed)
                {
                    logger.LogError(
                        "[WalletRefund] Refund failed for Order {OrderId}: {Error}",
                        notification.OrderId, result.Error);
                }
            }
            else
            {
                var releaseCommand = new ReleaseWalletReservationCommand(
                    notification.UserId,
                    notification.OrderId);

                await mediator.Send(releaseCommand, ct);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error handling wallet for cancelled order {OrderId}", notification.OrderId);
        }
    }
}