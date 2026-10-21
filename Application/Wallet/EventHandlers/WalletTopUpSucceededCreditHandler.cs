using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

/// <summary>
/// Records a completed wallet top-up.
///
/// The credit itself is applied synchronously by
/// <c>CompleteWalletTopUpHandler</c>, which is the single owner of that money
/// movement. Crediting it again from the domain event would credit the customer
/// twice for a single top-up.
/// </summary>
public sealed class WalletTopUpSucceededCreditHandler(
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletTopUpSucceededEvent>>
{
    public async Task Handle(
        DomainEventNotification<WalletTopUpSucceededEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;

        await auditService.LogInformationAsync(
            $"Wallet top-up succeeded. TopUpId={e.TopUpId}, Amount={e.Amount.Amount}, RefId={e.GatewayRefId}",
            ct);
    }
}
