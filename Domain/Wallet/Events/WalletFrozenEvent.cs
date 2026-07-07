using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletFrozenEvent(
    WalletId walletId,
    UserId ownerId,
    string reason,
    UserId frozenBy) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public string Reason { get; } = reason;
    public UserId FrozenBy { get; } = frozenBy;
}