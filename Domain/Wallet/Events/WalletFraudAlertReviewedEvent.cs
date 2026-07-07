using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletFraudAlertReviewedEvent(
    WalletFraudAlertId alertId,
    UserId reviewedBy,
    string? reviewNote,
    DateTime reviewedAt) : DomainEvent
{
    public WalletFraudAlertId AlertId { get; } = alertId;
    public UserId ReviewedBy { get; } = reviewedBy;
    public string? ReviewNote { get; } = reviewNote;
    public DateTime ReviewedAt { get; } = reviewedAt;
}