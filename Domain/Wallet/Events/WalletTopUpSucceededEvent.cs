using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTopUpSucceededEvent(WalletTopUpId topUpId, UserId userId, Money amount, string gatewayRefId) : DomainEvent
{
    public WalletTopUpId TopUpId { get; } = topUpId;
    public UserId UserId { get; } = userId;
    public Money Amount { get; } = amount;
    public string GatewayRefId { get; } = gatewayRefId;
}