namespace Domain.Wallet.Events;

public sealed record WalletReservationReleasedEvent(
    WalletId WalletId,
    UserId OwnerId,
    WalletReservationId ReservationId,
    Money Amount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}