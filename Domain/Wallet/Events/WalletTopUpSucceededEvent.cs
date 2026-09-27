using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTopUpSucceededEvent(WalletTopUpId TopUpId, UserId UserId, Money Amount, string GatewayRefId) : DomainEvent;
