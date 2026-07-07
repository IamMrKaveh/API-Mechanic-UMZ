using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletFraudAlertRaisedEvent(
    WalletFraudAlertId alertId,
    WalletId walletId,
    UserId userId,
    string ruleName,
    FraudAlertSeverity severity,
    string description,
    DateTime triggeredAt) : DomainEvent
{
    public WalletFraudAlertId AlertId { get; } = alertId;
    public WalletId WalletId { get; } = walletId;
    public UserId UserId { get; } = userId;
    public string RuleName { get; } = ruleName;
    public FraudAlertSeverity Severity { get; } = severity;
    public string Description { get; } = description;
    public DateTime TriggeredAt { get; } = triggeredAt;
}