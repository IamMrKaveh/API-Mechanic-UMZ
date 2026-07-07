using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WithdrawalCancelledEvent : DomainEvent
{
    public WalletWithdrawalRequestId WithdrawalId { get; }
    public UserId UserId { get; }

    public WithdrawalCancelledEvent(
        WalletWithdrawalRequestId withdrawalId,
        UserId userId)
    {
        WithdrawalId = withdrawalId;
        UserId = userId;
    }
}