using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Entities;

public sealed class WalletLedgerEntry : Entity<WalletLedgerEntryId>
{
    private WalletLedgerEntry()
    {
    }

    public Wallet.Aggregates.Wallet Wallet { get; private set; } = default!;
    public WalletId WalletId { get; private set; } = default!;
    public User.Aggregates.User Owner { get; private set; } = default!;
    public UserId OwnerId { get; private set; } = default!;

    public Money Amount { get; private set; } = default!;
    public Money BalanceAfter { get; private set; } = default!;
    public WalletTransactionType TransactionType { get; private set; }
    public string Description { get; private set; } = default!;
    public string ReferenceId { get; private set; } = default!;
    public string? IdempotencyKey { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public static WalletLedgerEntry Create(
        WalletLedgerEntryId id,
        WalletId walletId,
        UserId ownerId,
        Money amount,
        Money balanceAfter,
        WalletTransactionType transactionType,
        string description,
        string referenceId,
        string? idempotencyKey = null)
    {
        Guard.Against.Null(id, nameof(id));
        Guard.Against.Null(walletId, nameof(walletId));
        Guard.Against.Null(ownerId, nameof(ownerId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.Null(balanceAfter, nameof(balanceAfter));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        return new WalletLedgerEntry
        {
            Id = id,
            WalletId = walletId,
            OwnerId = ownerId,
            Amount = amount,
            BalanceAfter = balanceAfter,
            TransactionType = transactionType,
            Description = description,
            ReferenceId = referenceId,
            IdempotencyKey = idempotencyKey,
            OccurredAt = DateTime.UtcNow
        };
    }

    public bool IsCredit => TransactionType == WalletTransactionType.Credit;

    public bool IsDebit => TransactionType == WalletTransactionType.Debit || TransactionType == WalletTransactionType.ReservationConfirmed;

    public bool MatchesIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(IdempotencyKey))
            return false;
        return string.Equals(IdempotencyKey, key.Trim(), StringComparison.Ordinal);
    }
}