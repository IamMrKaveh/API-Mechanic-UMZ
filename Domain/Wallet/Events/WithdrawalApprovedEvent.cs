using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WithdrawalApprovedEvent : DomainEvent
{
    public WalletWithdrawalRequestId WithdrawalId { get; }
    public UserId UserId { get; }
    public UserId ApprovedBy { get; }

    public WithdrawalApprovedEvent(
        WalletWithdrawalRequestId withdrawalId,
        UserId userId,
        UserId approvedBy)
    {
        WithdrawalId = withdrawalId;
        UserId = userId;
        ApprovedBy = approvedBy;
    }
}