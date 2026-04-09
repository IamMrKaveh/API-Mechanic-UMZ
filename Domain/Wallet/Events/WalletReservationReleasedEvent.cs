using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletReservationReleasedEvent(
    WalletId walletId,
    UserId ownerId,
    WalletReservationId reservationId,
    Money amount) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public WalletReservationId ReservationId { get; } = reservationId;
    public Money Amount { get; } = amount;
}