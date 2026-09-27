using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletTransferInitiatedEvent(
    WalletTransferId TransferId,
    UserId FromUserId,
    UserId ToUserId,
    Money Amount,
    DateTime OtpExpiresAt) : DomainEvent;
