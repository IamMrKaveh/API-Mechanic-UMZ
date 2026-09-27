using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed record WalletFraudAlertRaisedEvent(
    WalletFraudAlertId AlertId,
    WalletId WalletId,
    UserId UserId,
    string RuleName,
    FraudAlertSeverity Severity,
    string Description,
    DateTime TriggeredAt) : DomainEvent;
