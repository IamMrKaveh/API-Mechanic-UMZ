using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Wallet.Entities;

public class WalletDebitRequestTests
{
    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    private static WalletDebitRequest BuildPending(
        WalletDebitRequestId? id = null,
        WalletId? walletId = null,
        UserId? ownerId = null,
        Money? amount = null,
        string reason = "purchase",
        string? description = null,
        UserId? requestedBy = null,
        WalletReservationId? reservationId = null,
        DateTime? expiresAt = null)
    {
        return WalletDebitRequest.Create(
            id ?? WalletDebitRequestId.NewId(),
            walletId ?? WalletId.NewId(),
            ownerId ?? UserId.NewId(),
            amount ?? Rial(100_000m),
            reason,
            description,
            requestedBy ?? UserId.NewId(),
            reservationId ?? WalletReservationId.NewId(),
            expiresAt ?? DateTime.UtcNow.AddHours(1));
    }

    // ---------- Create factory ----------

    [Fact]
    public void Create_WithValidInput_ReturnsPendingRequestWithExpectedState()
    {
        var id = WalletDebitRequestId.NewId();
        var walletId = WalletId.NewId();
        var ownerId = UserId.NewId();
        var amount = Rial(250_000m);
        var requestedBy = UserId.NewId();
        var reservationId = WalletReservationId.NewId();
        var expiresAt = DateTime.UtcNow.AddHours(2);

        var sut = WalletDebitRequest.Create(
            id, walletId, ownerId, amount, "reason", "desc", requestedBy, reservationId, expiresAt);

        sut.Id.ShouldBe(id);
        sut.WalletId.ShouldBe(walletId);
        sut.OwnerId.ShouldBe(ownerId);
        sut.Amount.ShouldBe(amount);
        sut.Reason.ShouldBe("reason");
        sut.Description.ShouldBe("desc");
        sut.RequestedBy.ShouldBe(requestedBy);
        sut.ReservationId.ShouldBe(reservationId);
        sut.ExpiresAt.ShouldBe(expiresAt);
        sut.Status.ShouldBe(WalletDebitRequestStatus.Pending);
        sut.RespondedAt.ShouldBeNull();
        sut.RespondedBy.ShouldBeNull();
        sut.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = BuildPending();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithNullDescription_LeavesDescriptionNull()
    {
        var sut = BuildPending(description: null);

        sut.Description.ShouldBeNull();
    }

    // ---------- Approve ----------

    [Fact]
    public void Approve_OnPending_TransitionsToApprovedAndRecordsResponder()
    {
        var sut = BuildPending();
        var approver = UserId.NewId();
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.Approve(approver);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(WalletDebitRequestStatus.Approved);
        sut.RespondedBy.ShouldBe(approver);
        sut.RespondedAt.ShouldNotBeNull();
        sut.RespondedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.RespondedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.RejectionReason.ShouldBeNull();
    }

    // ---------- Reject ----------

    [Fact]
    public void Reject_OnPending_TransitionsToRejectedAndCapturesReason()
    {
        var sut = BuildPending();
        var rejecter = UserId.NewId();

        sut.Reject(rejecter, "not approved");

        sut.Status.ShouldBe(WalletDebitRequestStatus.Rejected);
        sut.RespondedBy.ShouldBe(rejecter);
        sut.RejectionReason.ShouldBe("not approved");
        sut.RespondedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Reject_WithNullReason_StoresNullRejectionReason()
    {
        var sut = BuildPending();

        sut.Reject(UserId.NewId(), null);

        sut.RejectionReason.ShouldBeNull();
        sut.Status.ShouldBe(WalletDebitRequestStatus.Rejected);
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_OnPending_TransitionsToCancelledAndRecordsResponder()
    {
        var sut = BuildPending();
        var canceller = UserId.NewId();

        sut.Cancel(canceller);

        sut.Status.ShouldBe(WalletDebitRequestStatus.Cancelled);
        sut.RespondedBy.ShouldBe(canceller);
        sut.RespondedAt.ShouldNotBeNull();
    }

    // ---------- MarkExpired ----------

    [Fact]
    public void MarkExpired_OnPending_TransitionsToExpiredAndSetsRespondedAt()
    {
        var sut = BuildPending();
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.MarkExpired();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(WalletDebitRequestStatus.Expired);
        sut.RespondedAt.ShouldNotBeNull();
        sut.RespondedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.RespondedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.RespondedBy.ShouldBeNull();
    }

    // ---------- State transition matrix (entity does not gate state itself) ----------

    [Fact]
    public void Approve_AfterReject_OverwritesStatusToApproved()
    {
        // The entity-level methods do not guard current state; the aggregate is
        // responsible for those invariants. We document actual behavior here so
        // future guards would surface as a broken test.
        var sut = BuildPending();
        sut.Reject(UserId.NewId(), "no");
        var overwriter = UserId.NewId();

        sut.Approve(overwriter);

        sut.Status.ShouldBe(WalletDebitRequestStatus.Approved);
        sut.RespondedBy.ShouldBe(overwriter);
    }

    [Fact]
    public void Reject_AfterApprove_OverwritesStatusToRejected()
    {
        var sut = BuildPending();
        sut.Approve(UserId.NewId());
        var rejecter = UserId.NewId();

        sut.Reject(rejecter, "changed my mind");

        sut.Status.ShouldBe(WalletDebitRequestStatus.Rejected);
        sut.RespondedBy.ShouldBe(rejecter);
        sut.RejectionReason.ShouldBe("changed my mind");
    }

    [Fact]
    public void Cancel_AfterMarkExpired_OverwritesStatusToCancelled()
    {
        var sut = BuildPending();
        sut.MarkExpired();

        sut.Cancel(UserId.NewId());

        sut.Status.ShouldBe(WalletDebitRequestStatus.Cancelled);
    }
}
