using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletReservationCreatedEvent(
    WalletId walletId,
    UserId ownerId,
    WalletReservationId reservationId,
    Money amount,
    string purpose) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public WalletReservationId ReservationId { get; } = reservationId;
    public Money Amount { get; } = amount;
    public string Purpose { get; } = purpose;
}