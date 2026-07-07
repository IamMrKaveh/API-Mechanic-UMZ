using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WithdrawalRequestedEvent : DomainEvent
{
    public WalletWithdrawalRequestId WithdrawalId { get; }
    public UserId UserId { get; }
    public Money Amount { get; }
    public WalletReservationId ReservationId { get; }

    public WithdrawalRequestedEvent(
        WalletWithdrawalRequestId withdrawalId,
        UserId userId,
        Money amount,
        WalletReservationId reservationId)
    {
        WithdrawalId = withdrawalId;
        UserId = userId;
        Amount = amount;
        ReservationId = reservationId;
    }
}