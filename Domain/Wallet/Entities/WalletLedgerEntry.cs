using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Entities;

public sealed class WalletLedgerEntry : Entity<WalletLedgerEntryId>
{
    private WalletLedgerEntry()
    { }

    public WalletId WalletId { get; private set; } = default!;
    public UserId OwnerId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public Money BalanceAfter { get; private set; } = default!;
    public WalletTransactionType TransactionType { get; private set; }
    public string? Description { get; private set; }
    public string ReferenceId { get; private set; } = default!;
    public string? IdempotencyKey { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    public Aggregates.Wallet Wallet { get; private set; } = default!;
    public User.Aggregates.User Owner { get; private set; } = default!;

    private WalletLedgerEntry(
        WalletLedgerEntryId id,
        WalletId walletId,
        UserId ownerId,
        Money amount,
        Money balanceAfter,
        WalletTransactionType transactionType,
        string? description,
        string referenceId,
        string? idempotencyKey,
        string? correlationId,
        DateTime occurredAt)
    {
        Id = id;
        WalletId = walletId;
        OwnerId = ownerId;
        Amount = amount;
        BalanceAfter = balanceAfter;
        TransactionType = transactionType;
        Description = description?.Length > 500 ? description[..500] : description;
        ReferenceId = referenceId;
        IdempotencyKey = idempotencyKey?.Length > 200 ? idempotencyKey[..200] : idempotencyKey;
        CorrelationId = correlationId?.Length > 128 ? correlationId[..128] : correlationId;
        OccurredAt = occurredAt;
    }

    public static WalletLedgerEntry NewCredit(
        WalletId walletId,
        UserId ownerId,
        Money amount,
        Money balanceAfter,
        string? description,
        string referenceId,
        string? idempotencyKey,
        string? correlationId = null)
    {
        ValidateInputs(walletId, ownerId, amount, balanceAfter, referenceId);
        return new WalletLedgerEntry(
            WalletLedgerEntryId.NewId(),
            walletId,
            ownerId,
            amount,
            balanceAfter,
            WalletTransactionType.Credit,
            description,
            referenceId,
            idempotencyKey,
            correlationId,
            DateTime.UtcNow);
    }

    public static WalletLedgerEntry NewDebit(
        WalletId walletId,
        UserId ownerId,
        Money amount,
        Money balanceAfter,
        string? description,
        string referenceId,
        string? idempotencyKey,
        string? correlationId = null)
    {
        ValidateInputs(walletId, ownerId, amount, balanceAfter, referenceId);
        return new WalletLedgerEntry(
            WalletLedgerEntryId.NewId(),
            walletId,
            ownerId,
            amount,
            balanceAfter,
            WalletTransactionType.Debit,
            description,
            referenceId,
            idempotencyKey,
            correlationId,
            DateTime.UtcNow);
    }

    public static WalletLedgerEntry FromCreditEvent(WalletCreditedEvent evt)
    {
        Guard.Against.Null(evt, nameof(evt));
        return NewCredit(
            evt.WalletId,
            evt.OwnerId,
            evt.Amount,
            evt.NewBalance,
            evt.Description,
            evt.ReferenceId,
            evt.IdempotencyKey,
            evt.CorrelationId);
    }

    public static WalletLedgerEntry FromDebitEvent(WalletDebitedEvent evt)
    {
        Guard.Against.Null(evt, nameof(evt));
        return NewDebit(
            evt.WalletId,
            evt.OwnerId,
            evt.Amount,
            evt.NewBalance,
            evt.Description,
            evt.ReferenceId,
            evt.IdempotencyKey,
            evt.CorrelationId);
    }

    private static void ValidateInputs(
        WalletId walletId,
        UserId ownerId,
        Money amount,
        Money balanceAfter,
        string referenceId)
    {
        Guard.Against.Null(walletId, nameof(walletId));
        Guard.Against.Null(ownerId, nameof(ownerId));
        Guard.Against.Null(amount, nameof(amount));
        Guard.Against.Null(balanceAfter, nameof(balanceAfter));
        Guard.Against.NullOrWhiteSpace(referenceId, nameof(referenceId));

        if (amount.Amount <= 0)
            throw new DomainException("WalletLedgerEntry amount must be greater than zero.");

        if (referenceId.Length > 200)
            throw new DomainException("WalletLedgerEntry.ReferenceId exceeds 200 characters.");
    }
}
