using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WithdrawalPaidEvent(
    WalletWithdrawalRequestId WithdrawalId,
    UserId UserId,
    Money Amount,
    UserId PaidBy,
    string BankReferenceNumber) : DomainEvent;
