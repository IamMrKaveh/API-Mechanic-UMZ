namespace Domain.Wallet.Events;

public sealed record WalletReservationConfirmedEvent(
    WalletId WalletId,
    UserId OwnerId,
    WalletReservationId ReservationId,
    Money Amount,
    string Description,
    string ReferenceId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}