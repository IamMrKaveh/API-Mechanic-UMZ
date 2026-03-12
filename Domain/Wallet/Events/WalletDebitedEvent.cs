namespace Domain.Wallet.Events;

public sealed record WalletDebitedEvent(
    WalletId WalletId,
    UserId OwnerId,
    Money Amount,
    Money NewBalance,
    string Description,
    string ReferenceId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}