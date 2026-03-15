using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletCreditedEvent(
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