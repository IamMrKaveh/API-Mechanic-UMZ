using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTopUpSucceededEvent : DomainEvent
{
    public WalletTopUpId TopUpId { get; }
    public UserId UserId { get; }
    public Money Amount { get; }
    public string GatewayRefId { get; }

    public WalletTopUpSucceededEvent(WalletTopUpId topUpId, UserId userId, Money amount, string gatewayRefId)
    {
        TopUpId = topUpId;
        UserId = userId;
        Amount = amount;
        GatewayRefId = gatewayRefId;
    }
}