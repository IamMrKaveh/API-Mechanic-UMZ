using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletUnfrozenEvent(
    WalletId walletId,
    UserId ownerId,
    UserId unfrozenBy) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public UserId UnfrozenBy { get; } = unfrozenBy;
}