using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Events;

public sealed class WalletFraudAlertDismissedEvent(
    WalletFraudAlertId alertId,
    UserId dismissedBy,
    string? dismissNote,
    DateTime dismissedAt) : DomainEvent
{
    public WalletFraudAlertId AlertId { get; } = alertId;
    public UserId DismissedBy { get; } = dismissedBy;
    public string? DismissNote { get; } = dismissNote;
    public DateTime DismissedAt { get; } = dismissedAt;
}