using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletDebitRequestCreatedEvent(
    WalletId walletId,
    UserId ownerId,
    WalletDebitRequestId requestId,
    Money amount,
    string reason,
    UserId requestedBy) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public WalletDebitRequestId RequestId { get; } = requestId;
    public Money Amount { get; } = amount;
    public string Reason { get; } = reason;
    public UserId RequestedBy { get; } = requestedBy;
}
