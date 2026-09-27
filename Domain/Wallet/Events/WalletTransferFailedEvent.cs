using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTransferFailedEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId,
    string Reason) : DomainEvent;
