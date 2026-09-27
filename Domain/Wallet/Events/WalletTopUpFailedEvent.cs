using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTopUpFailedEvent(WalletTopUpId TopUpId, UserId UserId, string Reason) : DomainEvent;