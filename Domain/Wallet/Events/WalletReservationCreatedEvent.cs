using Domain.Common.Abstractions;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletReservationCreatedEvent(
    WalletId WalletId,
    UserId OwnerId,
    WalletReservationId ReservationId,
    Money Amount,
    string Purpose) : IDomainEvent;