using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletLedgerEntryCreatedEvent(
    WalletLedgerEntryId EntryId,
    WalletId WalletId,
    UserId OwnerId,
    Money Amount,
    Money BalanceAfter,
    WalletTransactionType TransactionType,
    string Description,
    string ReferenceId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}