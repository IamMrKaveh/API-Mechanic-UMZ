using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTransferInitiatedEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId,
    Money Amount,
    DateTime OtpExpiresAt) : DomainEvent
{
    public WalletTransferId TransferId { get; } = TransferId;
    public UserId FromUserId { get; } = FromUserId;
    public UserId ToUserId { get; } = ToUserId;
    public Money Amount { get; } = Amount;
    public DateTime OtpExpiresAt { get; } = OtpExpiresAt;
}