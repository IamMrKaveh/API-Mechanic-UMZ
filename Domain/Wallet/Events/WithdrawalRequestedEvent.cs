using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WithdrawalRequestedEvent(
    WalletWithdrawalRequestId WithdrawalId,
    UserId UserId,
    Money Amount,
    WalletReservationId ReservationId) : DomainEvent;