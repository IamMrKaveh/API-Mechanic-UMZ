using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletDebitRequestApprovedEvent(
    WalletId WalletId,
    UserId OwnerId,
    WalletDebitRequestId RequestId,
    Money Amount,
    UserId ApprovedBy) : DomainEvent;
