using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletDebitRequestRejectedEvent(
    WalletId WalletId,
    UserId OwnerId,
    WalletDebitRequestId RequestId,
    Money Amount,
    UserId RejectedBy,
    string? RejectionReason) : DomainEvent;
