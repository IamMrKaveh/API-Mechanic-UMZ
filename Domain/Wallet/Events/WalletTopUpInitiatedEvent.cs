using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletTopUpInitiatedEvent : DomainEvent
{
    public WalletTopUpId TopUpId { get; }
    public UserId UserId { get; }
    public Money Amount { get; }
    public string Gateway { get; }

    public WalletTopUpInitiatedEvent(WalletTopUpId topUpId, UserId userId, Money amount, string gateway)
    {
        TopUpId = topUpId;
        UserId = userId;
        Amount = amount;
        Gateway = gateway;
    }
}