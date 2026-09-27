using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletDebitRequestCreatedEvent(
    WalletId WalletId,
    UserId OwnerId,
    WalletDebitRequestId RequestId,
    Money Amount,
    string Reason,
    UserId RequestedBy) : DomainEvent;
