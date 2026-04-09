using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletLedgerEntryCreatedEvent(
    WalletLedgerEntryId entryId,
    WalletId walletId,
    UserId ownerId,
    Money amount,
    Money balanceAfter,
    WalletTransactionType transactionType,
    string description,
    string referenceId) : DomainEvent
{
    public WalletLedgerEntryId EntryId { get; } = entryId;
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public Money Amount { get; } = amount;
    public Money BalanceAfter { get; } = balanceAfter;
    public WalletTransactionType TransactionType { get; } = transactionType;
    public string Description { get; } = description;
    public string ReferenceId { get; } = referenceId;
}