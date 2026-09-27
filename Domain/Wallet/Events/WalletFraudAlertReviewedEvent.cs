using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletFraudAlertReviewedEvent(
    WalletFraudAlertId AlertId,
    UserId ReviewedBy,
    string? ReviewNote,
    DateTime ReviewedAt) : DomainEvent;
