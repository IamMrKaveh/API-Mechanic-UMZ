using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTransferCompletedEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId,
    Money Amount,
    string CorrelationId) : DomainEvent
{
    public WalletTransferId TransferId { get; } = TransferId;
    public UserId FromUserId { get; } = FromUserId;
    public UserId ToUserId { get; } = ToUserId;
    public Money Amount { get; } = Amount;
    public string CorrelationId { get; } = CorrelationId;
}