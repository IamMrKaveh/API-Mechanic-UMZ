using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTopUpFailedEvent : DomainEvent
{
    public WalletTopUpId TopUpId { get; }
    public UserId UserId { get; }
    public string Reason { get; }

    public WalletTopUpFailedEvent(WalletTopUpId topUpId, UserId userId, string reason)
    {
        TopUpId = topUpId;
        UserId = userId;
        Reason = reason;
    }
}