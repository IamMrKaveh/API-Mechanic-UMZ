using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WithdrawalCancelledEvent(
    WalletWithdrawalRequestId WithdrawalId,
    UserId UserId) : DomainEvent;