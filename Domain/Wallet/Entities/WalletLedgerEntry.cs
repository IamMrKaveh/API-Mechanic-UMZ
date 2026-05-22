using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Entities;

public sealed class WalletLedgerEntry : Entity<WalletLedgerEntryId>
{
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

    private WalletLedgerEntry()
    {
    }
}