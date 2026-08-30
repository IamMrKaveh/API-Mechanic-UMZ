using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Wallet.Aggregates;

public class WalletWithdrawalRequestTests
{
    private const decimal MinimumAmount = 50_000m;

    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    // ---------- Create factory ----------

    [Fact]
    public void Create_WithValidInput_ReturnsPendingWithdrawalRequest()
    {
        var userId = UserId.NewId();
        var amount = Rial(200_000m);
        var iban = new IbanNumberBuilder().Build();
        var reservationId = WalletReservationId.NewId();

        var sut = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(amount)
            .WithIban(iban)
            .WithAccountHolder("John Doe")
            .WithReservationId(reservationId)
            .WithDescription("payout")
            .Build();

        sut.Id.ShouldNotBeNull();
        sut.UserId.ShouldBe(userId);
        sut.Amount.ShouldBe(amount);
        sut.Iban.ShouldBe(iban);
        sut.AccountHolder.ShouldBe("John Doe");
        sut.ReservationId.ShouldBe(reservationId);
        sut.Description.ShouldBe("payout");
        sut.Status.ShouldBe(WalletWithdrawalStatus.Pending);
        sut.RejectionReason.ShouldBeNull();
        sut.BankReferenceNumber.ShouldBeNull();
        sut.ProcessedBy.ShouldBeNull();
        sut.ApprovedAt.ShouldBeNull();
        sut.RejectedAt.ShouldBeNull();
        sut.PaidAt.ShouldBeNull();
        sut.CancelledAt.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new WalletWithdrawalRequestBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_TrimsAccountHolder()
    {
        var sut = new WalletWithdrawalRequestBuilder().WithAccountHolder("   Alice   ").Build();

        sut.AccountHolder.ShouldBe("Alice");
    }

    [Fact]
    public void Create_TrimsDescription_WhenNonEmpty()
    {
        var sut = new WalletWithdrawalRequestBuilder().WithDescription("   note   ").Build();

        sut.Description.ShouldBe("note");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankDescription_LeavesDescriptionNull(string? description)
    {
        var sut = new WalletWithdrawalRequestBuilder().WithDescription(description).Build();

        sut.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_RaisesExactlyOneWithdrawalRequestedEvent()
    {
        var userId = UserId.NewId();
        var amount = Rial(200_000m);
        var reservationId = WalletReservationId.NewId();

        var sut = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(amount)
            .WithReservationId(reservationId)
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WithdrawalRequestedEvent>();
        evt.WithdrawalId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(userId);
        evt.Amount.ShouldBe(amount);
        evt.ReservationId.ShouldBe(reservationId);
    }

    [Fact]
    public void Create_IncrementsVersionToOne()
    {
        new WalletWithdrawalRequestBuilder().Build().Version.ShouldBe(1);
    }

    [Fact]
    public void Create_WithAmountEqualToMinimum_Succeeds()
    {
        var sut = new WalletWithdrawalRequestBuilder().WithAmount(Rial(MinimumAmount)).Build();

        sut.Amount.Amount.ShouldBe(MinimumAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(49_999)]
    public void Create_WithAmountBelowMinimum_ThrowsDomainException(decimal amount)
    {
        Should.Throw<DomainException>(() =>
            WalletWithdrawalRequest.Create(
                UserId.NewId(),
                Rial(amount),
                new IbanNumberBuilder().Build(),
                "holder",
                WalletReservationId.NewId()));
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletWithdrawalRequest.Create(
                null!,
                Rial(200_000m),
                new IbanNumberBuilder().Build(),
                "holder",
                WalletReservationId.NewId()));
    }

    [Fact]
    public void Create_WithNullAmount_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletWithdrawalRequest.Create(
                UserId.NewId(),
                null!,
                new IbanNumberBuilder().Build(),
                "holder",
                WalletReservationId.NewId()));
    }

    [Fact]
    public void Create_WithNullIban_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletWithdrawalRequest.Create(
                UserId.NewId(),
                Rial(200_000m),
                null!,
                "holder",
                WalletReservationId.NewId()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankAccountHolder_ThrowsDomainException(string? accountHolder)
    {
        Should.Throw<DomainException>(() =>
            WalletWithdrawalRequest.Create(
                UserId.NewId(),
                Rial(200_000m),
                new IbanNumberBuilder().Build(),
                accountHolder!,
                WalletReservationId.NewId()));
    }

    [Fact]
    public void Create_WithNullReservationId_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            WalletWithdrawalRequest.Create(
                UserId.NewId(),
                Rial(200_000m),
                new IbanNumberBuilder().Build(),
                "holder",
                null!));
    }

    // ---------- Approve ----------

    [Fact]
    public void Approve_OnPending_TransitionsToApprovedAndRaisesEvent()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.ClearDomainEvents();
        var admin = UserId.NewId();
        var versionBefore = sut.Version;
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.Approve(admin);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(WalletWithdrawalStatus.Approved);
        sut.ProcessedBy.ShouldBe(admin);
        sut.ApprovedAt.ShouldNotBeNull();
        sut.ApprovedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.ApprovedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WithdrawalApprovedEvent>();
        evt.WithdrawalId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.ApprovedBy.ShouldBe(admin);
    }

    [Fact]
    public void Approve_AfterApproved_ThrowsDomainException()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.Approve(UserId.NewId());

        Should.Throw<DomainException>(() => sut.Approve(UserId.NewId()));
    }

    [Fact]
    public void Approve_AfterRejected_ThrowsDomainException()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.Reject(UserId.NewId(), "reason");

        Should.Throw<DomainException>(() => sut.Approve(UserId.NewId()));
    }

    [Fact]
    public void Approve_AfterCancelled_ThrowsDomainException()
    {
        var owner = UserId.NewId();
        var sut = new WalletWithdrawalRequestBuilder().WithUserId(owner).Build();
        sut.Cancel(owner);

        Should.Throw<DomainException>(() => sut.Approve(UserId.NewId()));
    }

    // ---------- Reject ----------

    [Fact]
    public void Reject_OnPending_TransitionsToRejectedWithTrimmedReasonAndRaisesEvent()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.ClearDomainEvents();
        var admin = UserId.NewId();
        var versionBefore = sut.Version;

        sut.Reject(admin, "   invalid iban   ");

        sut.Status.ShouldBe(WalletWithdrawalStatus.Rejected);
        sut.RejectionReason.ShouldBe("invalid iban");
        sut.ProcessedBy.ShouldBe(admin);
        sut.RejectedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WithdrawalRejectedEvent>();
        evt.WithdrawalId.ShouldBe(sut.Id);
        evt.RejectedBy.ShouldBe(admin);
        evt.Reason.ShouldBe("invalid iban");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithBlankReason_ThrowsDomainException(string? reason)
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();

        Should.Throw<DomainException>(() => sut.Reject(UserId.NewId(), reason!));
    }

    [Fact]
    public void Reject_AfterApproved_ThrowsDomainException()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.Approve(UserId.NewId());

        Should.Throw<DomainException>(() => sut.Reject(UserId.NewId(), "reason"));
    }

    // ---------- MarkPaid ----------

    [Fact]
    public void MarkPaid_FromPending_TransitionsToPaidAndRaisesEvent()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.ClearDomainEvents();
        var admin = UserId.NewId();
        var versionBefore = sut.Version;

        sut.MarkPaid(admin, "  BANK-REF-1  ");

        sut.Status.ShouldBe(WalletWithdrawalStatus.Paid);
        sut.BankReferenceNumber.ShouldBe("BANK-REF-1");
        sut.ProcessedBy.ShouldBe(admin);
        sut.PaidAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WithdrawalPaidEvent>();
        evt.WithdrawalId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Amount.ShouldBe(sut.Amount);
        evt.PaidBy.ShouldBe(admin);
        evt.BankReferenceNumber.ShouldBe("BANK-REF-1");
    }

    [Fact]
    public void MarkPaid_FromApproved_TransitionsToPaidAndRaisesEvent()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.Approve(UserId.NewId());
        sut.ClearDomainEvents();

        sut.MarkPaid(UserId.NewId(), "BANK-REF-2");

        sut.Status.ShouldBe(WalletWithdrawalStatus.Paid);
        sut.DomainEvents.Single().ShouldBeOfType<WithdrawalPaidEvent>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkPaid_WithBlankBankReference_ThrowsDomainException(string? bankRef)
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.Approve(UserId.NewId());

        Should.Throw<DomainException>(() => sut.MarkPaid(UserId.NewId(), bankRef!));
    }

    [Fact]
    public void MarkPaid_AfterRejected_ThrowsDomainException()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.Reject(UserId.NewId(), "reason");

        Should.Throw<DomainException>(() => sut.MarkPaid(UserId.NewId(), "REF"));
    }

    [Fact]
    public void MarkPaid_AfterCancelled_ThrowsDomainException()
    {
        var owner = UserId.NewId();
        var sut = new WalletWithdrawalRequestBuilder().WithUserId(owner).Build();
        sut.Cancel(owner);

        Should.Throw<DomainException>(() => sut.MarkPaid(UserId.NewId(), "REF"));
    }

    [Fact]
    public void MarkPaid_AfterPaid_ThrowsDomainException()
    {
        var sut = new WalletWithdrawalRequestBuilder().Build();
        sut.MarkPaid(UserId.NewId(), "REF-1");

        Should.Throw<DomainException>(() => sut.MarkPaid(UserId.NewId(), "REF-2"));
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_ByOwnerOnPending_TransitionsToCancelledAndRaisesEvent()
    {
        var owner = UserId.NewId();
        var sut = new WalletWithdrawalRequestBuilder().WithUserId(owner).Build();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.Cancel(owner);

        sut.Status.ShouldBe(WalletWithdrawalStatus.Cancelled);
        sut.CancelledAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WithdrawalCancelledEvent>();
        evt.WithdrawalId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(owner);
    }

    [Fact]
    public void Cancel_ByNonOwner_ThrowsDomainException()
    {
        var owner = UserId.NewId();
        var sut = new WalletWithdrawalRequestBuilder().WithUserId(owner).Build();

        Should.Throw<DomainException>(() => sut.Cancel(UserId.NewId()));
    }

    [Fact]
    public void Cancel_AfterApproved_ThrowsDomainException()
    {
        var owner = UserId.NewId();
        var sut = new WalletWithdrawalRequestBuilder().WithUserId(owner).Build();
        sut.Approve(UserId.NewId());

        Should.Throw<DomainException>(() => sut.Cancel(owner));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsDomainException()
    {
        var owner = UserId.NewId();
        var sut = new WalletWithdrawalRequestBuilder().WithUserId(owner).Build();
        sut.Cancel(owner);

        Should.Throw<DomainException>(() => sut.Cancel(owner));
    }
}
