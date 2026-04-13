using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.Order.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class OrderCancelledWalletReleaseEventHandler(
    IMediator mediator,
    IWalletQueryService walletQueryService,
    IAuditService auditService) : INotificationHandler<OrderCancelledEvent>
{
    public async Task Handle(
        OrderCancelledEvent notification,
        CancellationToken ct)
    {
        try
        {
            var paymentEntry = await walletQueryService.GetOrderPaymentLedgerEntryAsync(
                notification.UserId, notification.OrderId, ct);

            if (paymentEntry != null)
            {
                var refundCommand = new CreditWalletCommand(
                    UserId: notification.UserId.Value,
                    Amount: paymentEntry.AmountDelta,
                    TransactionType: WalletTransactionType.Refund,
                    ReferenceType: WalletReferenceType.Order,
                    ReferenceId: notification.OrderId.Value.ToString(),
                    IdempotencyKey: $"refund-order-{notification.OrderId.Value}",
                    Description: $"استرداد وجه سفارش لغو شده #{notification.OrderId.Value}"
                );

                var result = await mediator.Send(refundCommand, ct);
                if (result.IsFailed)
                {
                    await auditService.LogSystemEventAsync(
                        "WalletRefundFailed",
                        $"استرداد کیف پول برای سفارش {notification.OrderId.Value} ناموفق بود: {result.Error}");
                }
            }
            else
            {
                var releaseCommand = new ReleaseWalletReservationCommand(
                    notification.UserId.Value,
                    notification.OrderId.Value);

                await mediator.Send(releaseCommand, ct);
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletOrderCancelledHandlerError",
                $"خطا در پردازش لغو سفارش {notification.OrderId.Value}: {ex.Message}");
        }
    }
}