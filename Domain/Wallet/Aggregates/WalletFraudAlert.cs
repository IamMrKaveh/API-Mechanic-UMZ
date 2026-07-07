using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Aggregates;

public sealed class WalletFraudAlert : AggregateRoot<WalletFraudAlertId>
{
    private WalletFraudAlert()
    { }

    public WalletId WalletId { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public string RuleName { get; private set; } = default!;
    public FraudAlertSeverity Severity { get; private set; }
    public string Description { get; private set; } = default!;
    public string? Metadata { get; private set; }
    public FraudAlertStatus Status { get; private set; }
    public DateTime TriggeredAt { get; private set; }
    public UserId? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static WalletFraudAlert Raise(
        WalletId walletId,
        UserId userId,
        string ruleName,
        FraudAlertSeverity severity,
        string description,
        string? metadata = null)
    {
        Guard.Against.Null(walletId, nameof(walletId));
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(ruleName, nameof(ruleName));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));

        var now = DateTime.UtcNow;
        var alert = new WalletFraudAlert
        {
            Id = WalletFraudAlertId.NewId(),
            WalletId = walletId,
            UserId = userId,
            RuleName = ruleName,
            Severity = severity,
            Description = description,
            Metadata = metadata,
            Status = FraudAlertStatus.Open,
            TriggeredAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        alert.RaiseDomainEvent(new WalletFraudAlertRaisedEvent(
            alert.Id, walletId, userId, ruleName, severity, description, now));

        return alert;
    }

    public void MarkAsReviewed(UserId reviewedBy, string? reviewNote)
    {
        Guard.Against.Null(reviewedBy, nameof(reviewedBy));

        if (Status != FraudAlertStatus.Open)
            throw new DomainException($"Fraud alert در وضعیت '{Status}' قابل بررسی نیست.");

        var now = DateTime.UtcNow;
        Status = FraudAlertStatus.Reviewed;
        ReviewedBy = reviewedBy;
        ReviewedAt = now;
        ReviewNote = reviewNote;
        UpdatedAt = now;

        RaiseDomainEvent(new WalletFraudAlertReviewedEvent(Id, reviewedBy, reviewNote, now));
    }

    public void Dismiss(UserId dismissedBy, string? dismissNote)
    {
        Guard.Against.Null(dismissedBy, nameof(dismissedBy));

        if (Status != FraudAlertStatus.Open)
            throw new DomainException($"Fraud alert در وضعیت '{Status}' قابل رد کردن نیست.");

        var now = DateTime.UtcNow;
        Status = FraudAlertStatus.Dismissed;
        ReviewedBy = dismissedBy;
        ReviewedAt = now;
        ReviewNote = dismissNote;
        UpdatedAt = now;

        RaiseDomainEvent(new WalletFraudAlertDismissedEvent(Id, dismissedBy, dismissNote, now));
    }
}