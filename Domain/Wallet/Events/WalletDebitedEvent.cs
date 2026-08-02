using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletDebitedEvent(
    WalletId walletId,
    UserId userId,
    Money amount,
    Money newBalance,
    string description,
    string referenceId,
    string? idempotencyKey = null,
    string? correlationId = null) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId UserId { get; } = userId;
    public UserId OwnerId => UserId;
    public Money Amount { get; } = amount;
    public Money NewBalance { get; } = newBalance;
    public string Description { get; } = description;
    public string ReferenceId { get; } = referenceId;
    public string? IdempotencyKey { get; } = idempotencyKey;
    public string? CorrelationId { get; } = correlationId;
}
