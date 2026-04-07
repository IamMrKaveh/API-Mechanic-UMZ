using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Domain.Common.Events;

namespace Domain.Wallet.Events;

public sealed class WalletStatusChangedEvent(
    WalletId walletId,
    UserId ownerId,
    WalletStatus newStatus,
    string? reason) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public WalletStatus NewStatus { get; } = newStatus;
    public string? Reason { get; } = reason;
}