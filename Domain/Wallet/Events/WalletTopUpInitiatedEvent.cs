using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTopUpInitiatedEvent(WalletTopUpId TopUpId, UserId UserId, Money Amount, string Gateway) : DomainEvent;