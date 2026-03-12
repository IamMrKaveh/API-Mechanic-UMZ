namespace Domain.Wallet.Events;

public sealed record WalletCreatedEvent(
    WalletId WalletId,
    UserId OwnerId,
    string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}