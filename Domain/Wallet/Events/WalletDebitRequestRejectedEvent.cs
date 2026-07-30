using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletDebitRequestRejectedEvent(
    WalletId walletId,
    UserId ownerId,
    WalletDebitRequestId requestId,
    Money amount,
    UserId rejectedBy,
    string? rejectionReason) : DomainEvent
{
    public WalletId WalletId { get; } = walletId;
    public UserId OwnerId { get; } = ownerId;
    public WalletDebitRequestId RequestId { get; } = requestId;
    public Money Amount { get; } = amount;
    public UserId RejectedBy { get; } = rejectedBy;
    public string? RejectionReason { get; } = rejectionReason;
}
