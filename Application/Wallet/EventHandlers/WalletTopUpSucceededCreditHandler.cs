using Application.Wallet.Features.Commands.CreditWallet;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class WalletTopUpSucceededCreditHandler(
    IMediator mediator,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletTopUpSucceededEvent>>
{
    public async Task Handle(
        DomainEventNotification<WalletTopUpSucceededEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        var idempotencyKey = $"topup:{e.TopUpId.Value:N}";

        var command = new CreditWalletCommand(
            UserId: e.UserId.Value,
            Amount: e.Amount.Amount,
            TransactionType: WalletTransactionType.Credit,
            ReferenceType: WalletReferenceType.TopUp,
            ReferenceId: e.TopUpId.Value.ToString(),
            IdempotencyKey: idempotencyKey,
            CorrelationId: e.GatewayRefId,
            Description: $"شارژ کیف پول از درگاه ({e.GatewayRefId})");

        var result = await mediator.Send(command, ct);

        if (result.IsFailed)
        {
            await auditService.LogErrorAsync(
                $"WalletTopUp credit application failed. TopUpId={e.TopUpId}, RefId={e.GatewayRefId}, Error={result.Error}",
                ct);
        }
        else
        {
            await auditService.LogInformationAsync(
                $"Wallet credited from top-up. TopUpId={e.TopUpId}, Amount={e.Amount.Amount}, RefId={e.GatewayRefId}",
                ct);
        }
    }
}