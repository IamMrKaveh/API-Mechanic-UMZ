using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletFrozenEvent(
    WalletId WalletId,
    UserId OwnerId,
    string Reason,
    UserId FrozenBy) : DomainEvent;
