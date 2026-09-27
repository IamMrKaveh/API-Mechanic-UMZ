using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTransferCancelledEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId) : DomainEvent;
