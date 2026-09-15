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
        DateTime expiresAt,
        DateTime now)
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
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
    }

    public void Approve(UserId approvedBy, DateTime now)
    {
        Status = WalletDebitRequestStatus.Approved;
        RespondedAt = now;
        RespondedBy = approvedBy;
    }

    public void Reject(UserId rejectedBy, string? rejectionReason, DateTime now)
    {
        Status = WalletDebitRequestStatus.Rejected;
        RespondedAt = now;
        RespondedBy = rejectedBy;
        RejectionReason = rejectionReason;
    }

    public void Cancel(UserId cancelledBy, DateTime now)
    {
        Status = WalletDebitRequestStatus.Cancelled;
        RespondedAt = now;
        RespondedBy = cancelledBy;
    }

    public void MarkExpired(DateTime now)
    {
        Status = WalletDebitRequestStatus.Expired;
        RespondedAt = now;
    }
}
