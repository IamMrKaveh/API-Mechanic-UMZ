using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Payment.Events;
using Domain.Wallet.Enums;

namespace Application.Wallet.EventHandlers;

public class PaymentSucceededWalletCreditEventHandler(
    IMediator mediator,
    IAuditService auditService) : INotificationHandler<DomainEventNotification<PaymentSucceededEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentSucceededEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        if (domainEvent.OrderId is null)
            return;

        try
        {
            var command = new CreditWalletCommand(
                UserId: domainEvent.UserId.Value,
                Amount: domainEvent.Amount.Amount,
                TransactionType: WalletTransactionType.Credit,
                ReferenceType: WalletReferenceType.Payment,
                ReferenceId: domainEvent.PaymentTransactionId.ToString(),
                IdempotencyKey: $"payment-topup-{domainEvent.PaymentTransactionId}",
                Description: "شارژ حساب از طریق درگاه پرداخت"
            );

            var result = await mediator.Send(command, ct);
            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "WalletTopUpFailed",
                    $"شارژ کیف پول برای تراکنش {domainEvent.PaymentTransactionId} ناموفق بود: {result.Error}",
                    ct);
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "WalletPaymentSucceededHandlerError",
                $"خطا در پردازش شارژ کیف پول برای تراکنش {domainEvent.PaymentTransactionId}: {ex.Message}",
                ct);
        }
    }
}