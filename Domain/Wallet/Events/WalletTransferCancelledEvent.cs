using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTransferCancelledEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId) : DomainEvent
{
    public WalletTransferId TransferId { get; } = TransferId;
    public UserId FromUserId { get; } = FromUserId;
    public UserId ToUserId { get; } = ToUserId;
}