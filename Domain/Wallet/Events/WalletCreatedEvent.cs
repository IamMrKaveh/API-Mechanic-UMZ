using Domain.Common.Abstractions;
using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletCreatedEvent(
    WalletId WalletId,
    UserId OwnerId,
    string Currency) : IDomainEvent;