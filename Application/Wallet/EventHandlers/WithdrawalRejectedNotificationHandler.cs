using Application.Common.Events;
using Application.Notification.Contracts;
using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class WithdrawalRejectedNotificationHandler(
    INotificationService notificationService)
    : INotificationHandler<DomainEventNotification<WithdrawalRejectedEvent>>
{
    public async Task Handle(
        DomainEventNotification<WithdrawalRejectedEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        try
        {
            await notificationService.CreateNotificationAsync(
                e.UserId,
                "درخواست برداشت رد شد",
                $"درخواست برداشت شما رد شد. دلیل: {e.Reason}",
                "WithdrawalRejected",
                $"/dashboard/wallet/withdrawals",
                e.WithdrawalId.Value,
                "Withdrawal",
                ct);
        }
        catch
        {
        }
    }
}