using Application.Wallet.Features.Commands.FreezeWallet;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class FraudAlertCriticalFreezeHandler(
    IMediator mediator,
    IAuditService auditService)
    : INotificationHandler<DomainEventNotification<WalletFraudAlertRaisedEvent>>
{
    public async Task Handle(
        DomainEventNotification<WalletFraudAlertRaisedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        if (evt.Severity != FraudAlertSeverity.Critical)
            return;

        try
        {
            var command = new FreezeWalletCommand(
                UserId: evt.UserId.Value,
                Reason: $"[Auto-Freeze] {evt.RuleName}: {evt.Description}");

            var result = await mediator.Send(command, ct);

            if (result.IsFailed)
            {
                await auditService.LogSystemEventAsync(
                    "FraudAutoFreezeFailed",
                    $"خودکار فریز کیف پول {evt.WalletId.Value} بر اساس هشدار {evt.AlertId.Value} ناموفق بود: {result.Error?.Message}",
                    ct);
            }
            else
            {
                await auditService.LogSystemEventAsync(
                    "FraudAutoFreezeApplied",
                    $"کیف پول {evt.WalletId.Value} به‌صورت خودکار بر اساس هشدار Critical {evt.RuleName} فریز شد.",
                    ct);
            }
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync(
                "FraudAutoFreezeError",
                $"خطا در فریز خودکار کیف پول {evt.WalletId.Value}: {ex.Message}",
                ct);
        }
    }
}