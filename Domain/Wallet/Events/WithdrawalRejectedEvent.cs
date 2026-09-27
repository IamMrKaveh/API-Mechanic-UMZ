using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WithdrawalRejectedEvent(
    WalletWithdrawalRequestId WithdrawalId,
    UserId UserId,
    UserId RejectedBy,
    string Reason) : DomainEvent;