using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletFraudAlertDismissedEvent(
    WalletFraudAlertId AlertId,
    UserId DismissedBy,
    string? DismissNote,
    DateTime DismissedAt) : DomainEvent;
