using Domain.Common.Abstractions;
using Domain.Common.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletDebitedEvent(
    WalletId WalletId,
    UserId OwnerId,
    Money Amount,
    Money NewBalance,
    string Description,
    string ReferenceId) : IDomainEvent;