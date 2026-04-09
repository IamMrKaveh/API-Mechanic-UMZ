using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletCreditedEvent(
    WalletId walletId,
    UserId ownerId,
    Money amount,
    Money newBalance,
    string description,
    string referenceId) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public Money Amount { get; } = amount;
    public Money NewBalance { get; } = newBalance;
    public string Description { get; } = description;
    public string ReferenceId { get; } = referenceId;
}