using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WithdrawalApprovedEvent(
    WalletWithdrawalRequestId WithdrawalId,
    UserId UserId,
    UserId ApprovedBy) : DomainEvent;