using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;

namespace Domain.Wallet.Entities;

public sealed class WalletDebitRequest : Entity<WalletDebitRequestId>
{
    private WalletDebitRequest()
    { }

    public WalletId WalletId { get; private set; } = default!;
    public UserId OwnerId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public string Reason { get; private set; } = default!;
    public string? Description { get; private set; }
    public UserId RequestedBy { get; private set; } = default!;
    public WalletReservationId ReservationId { get; private set; } = default!;
    public WalletDebitRequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public UserId? RespondedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    public static WalletDebitRequest Create(
        WalletDebitRequestId id,
        WalletId walletId,
        UserId ownerId,
        Money amount,
        string reason,
        string? description,
        UserId requestedBy,
        WalletReservationId reservationId,
        DateTime expiresAt)
    {
        return new WalletDebitRequest
        {
            Id = id,
            WalletId = walletId,
            OwnerId = ownerId,
            Amount = amount,
            Reason = reason,
            Description = description,
            RequestedBy = requestedBy,
            ReservationId = reservationId,
            Status = WalletDebitRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void Approve(UserId approvedBy)
    {
        Status = WalletDebitRequestStatus.Approved;
        RespondedAt = DateTime.UtcNow;
        RespondedBy = approvedBy;
    }

    public void Reject(UserId rejectedBy, string? rejectionReason)
    {
        Status = WalletDebitRequestStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
        RespondedBy = rejectedBy;
        RejectionReason = rejectionReason;
    }

    public void Cancel(UserId cancelledBy)
    {
        Status = WalletDebitRequestStatus.Cancelled;
        RespondedAt = DateTime.UtcNow;
        RespondedBy = cancelledBy;
    }

    public void MarkExpired()
    {
        Status = WalletDebitRequestStatus.Expired;
        RespondedAt = DateTime.UtcNow;
    }
}
