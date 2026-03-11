using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Aggregates;

public sealed class WalletLedgerEntry : AggregateRoot<WalletLedgerEntryId>
{
    private WalletLedgerEntry()
    { }

    public WalletId WalletId { get; private set; } = default!;
    public UserId OwnerId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public Money BalanceAfter { get; private set; } = default!;
    public WalletTransactionType TransactionType { get; private set; }
    public string Description { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;
    public DateTime OccurredAt { get; private set; }

    public static WalletLedgerEntry Create(
        WalletLedgerEntryId id,
        WalletId walletId,
        UserId ownerId,
        Money amount,
        Money balanceAfter,
        WalletTransactionType transactionType,
        string description,
        string referenceId)
    {
        var entry = new WalletLedgerEntry
        {
            Id = id,
            WalletId = walletId,
            OwnerId = ownerId,
            Amount = amount,
            BalanceAfter = balanceAfter,
            TransactionType = transactionType,
            Description = description,
            ReferenceId = referenceId,
            OccurredAt = DateTime.UtcNow
        };

        entry.RaiseDomainEvent(new WalletLedgerEntryCreatedEvent(
            id, walletId, ownerId, amount, balanceAfter, transactionType, description, referenceId));

        return entry;
    }
}