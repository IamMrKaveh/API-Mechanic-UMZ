using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WithdrawalPaidEvent(
    WalletWithdrawalRequestId withdrawalId,
    UserId userId,
    Money amount,
    UserId paidBy,
    string bankReferenceNumber) : DomainEvent
{
    public WalletWithdrawalRequestId WithdrawalId { get; } = withdrawalId;
    public UserId UserId { get; } = userId;
    public Money Amount { get; } = amount;
    public UserId PaidBy { get; } = paidBy;
    public string BankReferenceNumber { get; } = bankReferenceNumber;
}
