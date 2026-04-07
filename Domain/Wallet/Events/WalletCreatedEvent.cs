using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;
using Domain.Common.Events;

namespace Domain.Wallet.Events;

public sealed class WalletCreatedEvent(
    WalletId walletId,
    UserId ownerId,
    string currency) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public string Currency { get; } = currency;
}