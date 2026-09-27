using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTransferCompletedEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId,
    Money Amount,
    string CorrelationId) : DomainEvent
{
    public new string CorrelationId { get; } = CorrelationId;
}
