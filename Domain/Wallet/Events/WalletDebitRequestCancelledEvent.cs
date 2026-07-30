using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletDebitRequestCancelledEvent(
    WalletId walletId,
    UserId ownerId,
    WalletDebitRequestId requestId,
    Money amount,
    UserId cancelledBy) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public WalletDebitRequestId RequestId { get; } = requestId;
    public Money Amount { get; } = amount;
    public UserId CancelledBy { get; } = cancelledBy;
}
