using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.Order.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class OrderCancelledWalletReleaseEventHandler(
    IMediator mediator,
    IWalletQueryService walletQueryService,
    IAuditService auditService) : INotificationHandler<DomainEventNotification<OrderCancelledEvent>>
{
    public async Task Handle(
        DomainEventNotification<OrderCancelledEvent> notification,
        CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        try
        {
            var paymentEntry = await walletQueryService.GetOrderPaymentLedgerEntryAsync(
                domainEvent.UserId, domainEvent.OrderId, ct);

            if (paymentEntry != null)
            {
                var refundCommand = new CreditWalletCommand(
                    UserId: domainEvent.UserId.Value,
                    Amount: paymentEntry.AmountDelta,
                    TransactionType: WalletTransactionType.Refund,
                    ReferenceType: WalletReferenceType.Order,
                    ReferenceId: domainEvent.OrderId.Value.ToString(),
                    IdempotencyKey: $"refund-order-{domainEvent.OrderId.Value}",
                    Description: $"استرداد وجه سفارش لغو شده #{domainEvent.OrderId.Value}"
                );

                var result = await mediator.Send(refundCommand, ct);
                if (result.IsFailed)
                {
                    await auditService.LogSystemEventAsync(
                        "WalletRefundFailed",
                        $"استرداد کیف پول برای سفارش {domainEvent.OrderId.Value} ناموفق بود: {result.Error}");
                }
            }
            else
            {
                var releaseCommand = new ReleaseWalletReservationCommand(
                    domainEvent.UserId.Value,
                    domainEvent.OrderId.Value);

                await mediator.Send(releaseCommand, ct);
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletOrderCancelledHandlerError",
                $"خطا در پردازش لغو سفارش {domainEvent.OrderId.Value}: {ex.Message}",
                ct);
        }
    }
}