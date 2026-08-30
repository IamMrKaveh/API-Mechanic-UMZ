using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.Events;
using Domain.Wallet.Exceptions;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Wallet.Aggregates;

public class WalletTransferTests
{
    private const decimal MinimumAmount = 10_000m;
    private const int MaxOtpAttempts = 5;
    private const string ValidOtpHash = "0123456789abcdef0123456789abcdef";

    private static Money Rial(decimal amount) => Money.Create(amount, "IRT");

    private static WalletTransfer BuildPending(
        UserId? fromUserId = null,
        UserId? toUserId = null,
        Money? amount = null,
        string otpHash = ValidOtpHash,
        TimeSpan? ttl = null,
        string? description = null)
    {
        return new WalletTransferBuilder()
            .FromUser(fromUserId ?? UserId.NewId())
            .ToUser(toUserId ?? UserId.NewId())
            .WithAmount(amount ?? Rial(50_000m))
            .WithOtpHash(otpHash)
            .WithOtpTtl(ttl ?? TimeSpan.FromMinutes(5))
            .WithDescription(description)
            .Build();
    }

    // ---------- Initiate factory ----------

    [Fact]
    public void Initiate_WithValidInput_ReturnsPendingOtpTransfer()
    {
        var from = UserId.NewId();
        var to = UserId.NewId();
        var amount = Rial(50_000m);

        var sut = BuildPending(from, to, amount, description: "salary");

        sut.Id.ShouldNotBeNull();
        sut.FromUserId.ShouldBe(from);
        sut.ToUserId.ShouldBe(to);
        sut.Amount.ShouldBe(amount);
        sut.Status.ShouldBe(WalletTransferStatus.PendingOtp);
        sut.OtpHash.ShouldBe(ValidOtpHash);
        sut.OtpAttempts.ShouldBe(0);
        sut.Description.ShouldBe("salary");
        sut.CompletedAt.ShouldBeNull();
        sut.CancelledAt.ShouldBeNull();
        sut.FailureReason.ShouldBeNull();
        sut.CorrelationId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Initiate_SetsCreatedAtAndOtpExpiresAtCorrectly()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var ttl = TimeSpan.FromMinutes(10);

        var sut = BuildPending(ttl: ttl);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.OtpExpiresAt.ShouldBeGreaterThanOrEqualTo(before.Add(ttl));
        sut.OtpExpiresAt.ShouldBeLessThanOrEqualTo(after.Add(ttl));
    }

    [Fact]
    public void Initiate_TrimsDescription_WhenSurroundedByWhitespace()
    {
        var sut = BuildPending(description: "   note   ");

        sut.Description.ShouldBe("note");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Initiate_WithBlankDescription_LeavesDescriptionNull(string? description)
    {
        var sut = BuildPending(description: description);

        sut.Description.ShouldBeNull();
    }

    [Fact]
    public void Initiate_SetsCorrelationIdBasedOnTransferId()
    {
        var sut = BuildPending();

        sut.CorrelationId.ShouldBe(sut.Id.Value.ToString("N"));
    }

    [Fact]
    public void Initiate_RaisesExactlyOneWalletTransferInitiatedEvent()
    {
        var sut = BuildPending();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTransferInitiatedEvent>();
        evt.TransferId.ShouldBe(sut.Id);
        evt.FromUserId.ShouldBe(sut.FromUserId);
        evt.ToUserId.ShouldBe(sut.ToUserId);
        evt.Amount.ShouldBe(sut.Amount);
        evt.OtpExpiresAt.ShouldBe(sut.OtpExpiresAt);
    }

    [Fact]
    public void Initiate_SelfTransfer_ThrowsInvalidWalletTransferException()
    {
        var user = UserId.NewId();

        Should.Throw<InvalidWalletTransferException>(() =>
            WalletTransfer.Initiate(user, user, Rial(50_000m), ValidOtpHash, TimeSpan.FromMinutes(5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9_999)]
    public void Initiate_WithAmountBelowMinimum_ThrowsInvalidWalletTransferException(decimal amount)
    {
        Should.Throw<InvalidWalletTransferException>(() =>
            WalletTransfer.Initiate(UserId.NewId(), UserId.NewId(), Rial(amount), ValidOtpHash, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Initiate_WithAmountEqualToMinimum_Succeeds()
    {
        var sut = BuildPending(amount: Rial(MinimumAmount));

        sut.Amount.Amount.ShouldBe(MinimumAmount);
    }

    [Fact]
    public void Initiate_WithZeroTtl_ThrowsInvalidWalletTransferException()
    {
        Should.Throw<InvalidWalletTransferException>(() =>
            WalletTransfer.Initiate(UserId.NewId(), UserId.NewId(), Rial(50_000m), ValidOtpHash, TimeSpan.Zero));
    }

    [Fact]
    public void Initiate_WithNegativeTtl_ThrowsInvalidWalletTransferException()
    {
        Should.Throw<InvalidWalletTransferException>(() =>
            WalletTransfer.Initiate(UserId.NewId(), UserId.NewId(), Rial(50_000m), ValidOtpHash, TimeSpan.FromMinutes(-1)));
    }

    [Fact]
    public void Initiate_WithNullFromUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletTransfer.Initiate(null!, UserId.NewId(), Rial(50_000m), ValidOtpHash, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Initiate_WithNullToUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletTransfer.Initiate(UserId.NewId(), null!, Rial(50_000m), ValidOtpHash, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Initiate_WithNullAmount_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            WalletTransfer.Initiate(UserId.NewId(), UserId.NewId(), null!, ValidOtpHash, TimeSpan.FromMinutes(5)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Initiate_WithBlankOtpHash_ThrowsArgumentException(string? otpHash)
    {
        Should.Throw<ArgumentException>(() =>
            WalletTransfer.Initiate(UserId.NewId(), UserId.NewId(), Rial(50_000m), otpHash!, TimeSpan.FromMinutes(5)));
    }

    // ---------- VerifyOtp ----------

    [Fact]
    public void VerifyOtp_WithCorrectHashOnPending_DoesNotThrowAndKeepsPendingOtpStatus()
    {
        var sut = BuildPending();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        Should.NotThrow(() => sut.VerifyOtp(ValidOtpHash));

        sut.Status.ShouldBe(WalletTransferStatus.PendingOtp);
        sut.OtpAttempts.ShouldBe(0);
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void VerifyOtp_WithWrongHash_IncrementsAttemptsAndThrowsOtpMismatch()
    {
        var sut = BuildPending();

        Should.Throw<WalletTransferOtpMismatchException>(() => sut.VerifyOtp("wrong"));

        sut.OtpAttempts.ShouldBe(1);
        sut.Status.ShouldBe(WalletTransferStatus.PendingOtp);
    }

    [Fact]
    public void VerifyOtp_WithWrongHashUpToMaxAttempts_TransitionsToFailedOnFinalMismatch()
    {
        var sut = BuildPending();

        for (int i = 1; i < MaxOtpAttempts; i++)
        {
            Should.Throw<WalletTransferOtpMismatchException>(() => sut.VerifyOtp("wrong"));
            sut.Status.ShouldBe(WalletTransferStatus.PendingOtp);
        }

        sut.ClearDomainEvents();

        Should.Throw<WalletTransferOtpMismatchException>(() => sut.VerifyOtp("wrong"));

        sut.OtpAttempts.ShouldBe(MaxOtpAttempts);
        sut.Status.ShouldBe(WalletTransferStatus.Failed);
        sut.FailureReason.ShouldNotBeNullOrWhiteSpace();
        sut.DomainEvents.OfType<WalletTransferFailedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void VerifyOtp_OnExpiredOtp_TransitionsToExpiredAndThrows()
    {
        var sut = BuildPending(ttl: TimeSpan.FromMilliseconds(1));
        Thread.Sleep(50);
        sut.ClearDomainEvents();

        Should.Throw<InvalidWalletTransferException>(() => sut.VerifyOtp(ValidOtpHash));

        sut.Status.ShouldBe(WalletTransferStatus.Expired);
        sut.FailureReason.ShouldNotBeNullOrWhiteSpace();
        sut.DomainEvents.OfType<WalletTransferFailedEvent>().Count().ShouldBe(1);
    }

    [Fact]
    public void VerifyOtp_AfterCancelled_ThrowsInvalidWalletTransferException()
    {
        var from = UserId.NewId();
        var sut = BuildPending(fromUserId: from);
        sut.Cancel(from);

        Should.Throw<InvalidWalletTransferException>(() => sut.VerifyOtp(ValidOtpHash));
    }

    [Fact]
    public void VerifyOtp_AfterCompleted_ThrowsInvalidWalletTransferException()
    {
        var sut = BuildPending();
        sut.MarkCompleted();

        Should.Throw<InvalidWalletTransferException>(() => sut.VerifyOtp(ValidOtpHash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VerifyOtp_WithBlankHash_ThrowsArgumentException(string? otpHash)
    {
        var sut = BuildPending();

        Should.Throw<ArgumentException>(() => sut.VerifyOtp(otpHash!));
    }

    // ---------- MarkCompleted ----------

    [Fact]
    public void MarkCompleted_OnPending_TransitionsToCompletedAndRaisesEvent()
    {
        var sut = BuildPending();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.MarkCompleted();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(WalletTransferStatus.Completed);
        sut.CompletedAt.ShouldNotBeNull();
        sut.CompletedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.CompletedAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTransferCompletedEvent>();
        evt.TransferId.ShouldBe(sut.Id);
        evt.FromUserId.ShouldBe(sut.FromUserId);
        evt.ToUserId.ShouldBe(sut.ToUserId);
        evt.Amount.ShouldBe(sut.Amount);
        evt.CorrelationId.ShouldBe(sut.CorrelationId);
    }

    [Fact]
    public void MarkCompleted_AlreadyCompleted_ThrowsInvalidWalletTransferException()
    {
        var sut = BuildPending();
        sut.MarkCompleted();

        Should.Throw<InvalidWalletTransferException>(() => sut.MarkCompleted());
    }

    [Fact]
    public void MarkCompleted_AfterCancelled_ThrowsInvalidWalletTransferException()
    {
        var from = UserId.NewId();
        var sut = BuildPending(fromUserId: from);
        sut.Cancel(from);

        Should.Throw<InvalidWalletTransferException>(() => sut.MarkCompleted());
    }

    // ---------- Cancel ----------

    [Fact]
    public void Cancel_ByCreatorOnPending_TransitionsToCancelledAndRaisesEvent()
    {
        var from = UserId.NewId();
        var sut = BuildPending(fromUserId: from);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.Cancel(from);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Status.ShouldBe(WalletTransferStatus.Cancelled);
        sut.CancelledAt.ShouldNotBeNull();
        sut.CancelledAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.CancelledAt.Value.ShouldBeLessThanOrEqualTo(after);
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTransferCancelledEvent>();
        evt.TransferId.ShouldBe(sut.Id);
        evt.FromUserId.ShouldBe(from);
        evt.ToUserId.ShouldBe(sut.ToUserId);
    }

    [Fact]
    public void Cancel_ByNonCreator_ThrowsInvalidWalletTransferException()
    {
        var sut = BuildPending();

        Should.Throw<InvalidWalletTransferException>(() => sut.Cancel(UserId.NewId()));
    }

    [Fact]
    public void Cancel_WithNullRequester_ThrowsArgumentNullException()
    {
        var sut = BuildPending();

        Should.Throw<ArgumentNullException>(() => sut.Cancel(null!));
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsInvalidWalletTransferException()
    {
        var from = UserId.NewId();
        var sut = BuildPending(fromUserId: from);
        sut.Cancel(from);

        Should.Throw<InvalidWalletTransferException>(() => sut.Cancel(from));
    }

    [Fact]
    public void Cancel_AfterCompleted_ThrowsInvalidWalletTransferException()
    {
        var from = UserId.NewId();
        var sut = BuildPending(fromUserId: from);
        sut.MarkCompleted();

        Should.Throw<InvalidWalletTransferException>(() => sut.Cancel(from));
    }

    // ---------- MarkFailed ----------

    [Fact]
    public void MarkFailed_OnPending_TransitionsToFailedAndRaisesEvent()
    {
        var sut = BuildPending();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        sut.MarkFailed("technical error");

        sut.Status.ShouldBe(WalletTransferStatus.Failed);
        sut.FailureReason.ShouldBe("technical error");
        sut.CompletedAt.ShouldNotBeNull();
        sut.Version.ShouldBe(versionBefore + 1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<WalletTransferFailedEvent>();
        evt.TransferId.ShouldBe(sut.Id);
        evt.Reason.ShouldBe("technical error");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkFailed_WithBlankReason_ThrowsArgumentException(string? reason)
    {
        var sut = BuildPending();

        Should.Throw<ArgumentException>(() => sut.MarkFailed(reason!));
    }

    [Fact]
    public void MarkFailed_OnAlreadyCompletedTransfer_IsSilentNoOp()
    {
        var sut = BuildPending();
        sut.MarkCompleted();
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        Should.NotThrow(() => sut.MarkFailed("late"));

        sut.Status.ShouldBe(WalletTransferStatus.Completed);
        sut.FailureReason.ShouldBeNull();
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkFailed_OnAlreadyCancelledTransfer_IsSilentNoOp()
    {
        var from = UserId.NewId();
        var sut = BuildPending(fromUserId: from);
        sut.Cancel(from);
        sut.ClearDomainEvents();
        var versionBefore = sut.Version;

        Should.NotThrow(() => sut.MarkFailed("late"));

        sut.Status.ShouldBe(WalletTransferStatus.Cancelled);
        sut.Version.ShouldBe(versionBefore);
        sut.DomainEvents.ShouldBeEmpty();
    }
}
