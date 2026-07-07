using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WithdrawalPaidEvent : DomainEvent
{
    public WalletWithdrawalRequestId WithdrawalId { get; }
    public UserId UserId { get; }
    public Money Amount { get; }
    public UserId PaidBy { get; }
    public string BankReferenceNumber { get; }

    public WithdrawalPaidEvent(
        WalletWithdrawalRequestId withdrawalId,
        UserId userId,
        Money amount,
        UserId paidBy,
        string bankReferenceNumber)
    {
        WithdrawalId = withdrawalId;
        UserId = userId;
        Amount = amount;
        PaidBy = paidBy;
        BankReferenceNumber = bankReferenceNumber;
    }
}