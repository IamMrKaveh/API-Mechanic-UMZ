using Application.Common.Events;
using Application.Notification.Contracts;
using Domain.Wallet.Events;

namespace Application.Wallet.EventHandlers;

public sealed class WithdrawalPaidNotificationHandler(
    INotificationService notificationService)
    : INotificationHandler<DomainEventNotification<WithdrawalPaidEvent>>
{
    public async Task Handle(
        DomainEventNotification<WithdrawalPaidEvent> notification,
        CancellationToken ct)
    {
        var e = notification.DomainEvent;
        try
        {
            await notificationService.CreateNotificationAsync(
                e.UserId,
                "برداشت شما پرداخت شد",
                $"مبلغ {e.Amount.Amount:N0} تومان به حساب بانکی شما واریز شد. شماره پیگیری: {e.BankReferenceNumber}",
                "WithdrawalPaid",
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