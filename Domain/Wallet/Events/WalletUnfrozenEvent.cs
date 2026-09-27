using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletUnfrozenEvent(
    WalletId WalletId,
    UserId OwnerId,
    UserId UnfrozenBy,
    string Reason) : DomainEvent;
