using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTransferFailedEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId,
    string Reason) : DomainEvent
{
    public WalletTransferId TransferId { get; } = TransferId;
    public UserId FromUserId { get; } = FromUserId;
    public UserId ToUserId { get; } = ToUserId;
    public string Reason { get; } = Reason;
}