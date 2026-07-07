using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WithdrawalRejectedEvent : DomainEvent
{
    public WalletWithdrawalRequestId WithdrawalId { get; }
    public UserId UserId { get; }
    public UserId RejectedBy { get; }
    public string Reason { get; }

    public WithdrawalRejectedEvent(
        WalletWithdrawalRequestId withdrawalId,
        UserId userId,
        UserId rejectedBy,
        string reason)
    {
        WithdrawalId = withdrawalId;
        UserId = userId;
        RejectedBy = rejectedBy;
        Reason = reason;
    }
}