using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Events;
using Domain.Security.Exceptions;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Security.Aggregates;

public class UserOtpTests
{
    private static readonly DateTime Now = new(2026, 8, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidInput_InitializesAllStateFields()
    {
        var userId = UserId.NewId();
        var code = OtpCode.Create("135790");
        var validity = TimeSpan.FromMinutes(5);

        var sut = new UserOtpBuilder()
            .WithUserId(userId)
            .WithCode(code)
            .WithPurpose(OtpPurpose.Login)
            .WithValidity(validity)
            .Build(Now);

        sut.Id.ShouldNotBeNull();
        sut.Id.Value.ShouldNotBe(Guid.Empty);
        sut.UserId.ShouldBe(userId);
        sut.CodeHash.ShouldBe(code.ToHash());
        sut.CodeHash.ShouldNotContain("135790");
        sut.Purpose.ShouldBe(OtpPurpose.Login);
        sut.IsVerified.ShouldBeFalse();
        sut.VerificationAttempts.ShouldBe(0);
        sut.RemainingAttempts.ShouldBe(5);
        sut.IsLockedOut.ShouldBeFalse();
        sut.IsExpired(Now).ShouldBeFalse();
        sut.IsUsable(Now).ShouldBeTrue();
        sut.VerifiedAt.ShouldBeNull();
        sut.CreatedAt.ShouldBe(Now);
        sut.ExpiresAt.ShouldBe(Now.Add(validity));
        sut.ExpiresAt.ShouldBeGreaterThan(sut.CreatedAt);
    }

    [Fact]
    public void Create_RaisesExactlyOneOtpGeneratedEventWithMatchingFields()
    {
        var userId = UserId.NewId();

        var sut = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.PasswordReset)
            .Build(Now);

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OtpGeneratedEvent>();
        evt.OtpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(userId);
        evt.Purpose.ShouldBe(OtpPurpose.PasswordReset);
        evt.ExpiresAt.ShouldBe(sut.ExpiresAt);
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserOtp.Create(null!, OtpCode.Create("135790"), OtpPurpose.Login, TimeSpan.FromMinutes(5), Now));
    }

    [Fact]
    public void Create_WithNullCode_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserOtp.Create(UserId.NewId(), null!, OtpPurpose.Login, TimeSpan.FromMinutes(5), Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-3600)]
    public void Create_WithZeroOrNegativeValidity_ThrowsDomainException(int seconds)
    {
        Should.Throw<DomainException>(() =>
            UserOtp.Create(UserId.NewId(), OtpCode.Create("135790"), OtpPurpose.Login, TimeSpan.FromSeconds(seconds), Now));
    }

    [Fact]
    public void Create_WithValidityAboveThirtyMinutes_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            UserOtp.Create(UserId.NewId(), OtpCode.Create("135790"), OtpPurpose.Login, TimeSpan.FromMinutes(31), Now));
    }

    [Fact]
    public void Create_WithValidityAtExactlyThirtyMinutes_Succeeds()
    {
        Should.NotThrow(() =>
            UserOtp.Create(UserId.NewId(), OtpCode.Create("135790"), OtpPurpose.Login, TimeSpan.FromMinutes(30), Now));
    }

    [Fact]
    public void GetTimeUntilExpiry_OnFreshOtp_ReturnsPositiveRemainingTime()
    {
        var sut = new UserOtpBuilder().WithValidity(TimeSpan.FromMinutes(5)).Build(Now);

        sut.GetTimeUntilExpiry(Now).ShouldNotBeNull();
        sut.GetTimeUntilExpiry(Now)!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        sut.GetTimeUntilExpiry(Now)!.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Verify_WithCorrectCode_MarksVerifiedAndRaisesOtpVerifiedEvent()
    {
        var code = OtpCode.Create("135790");
        var sut = new UserOtpBuilder().WithCode(code).Build(Now);
        sut.ClearDomainEvents();

        sut.Verify(code, Now);

        sut.IsVerified.ShouldBeTrue();
        sut.VerifiedAt.ShouldBe(Now);
        sut.VerificationAttempts.ShouldBe(1);
        sut.IsUsable(Now).ShouldBeFalse();
        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OtpVerifiedEvent>();
        evt.OtpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Purpose.ShouldBe(sut.Purpose);
    }

    [Fact]
    public void Verify_WithWrongCode_IncrementsAttemptsRaisesFailedEventAndThrows()
    {
        var sut = new UserOtpBuilder().WithCode("135790").Build(Now);
        sut.ClearDomainEvents();

        Should.Throw<InvalidOtpCodeException>(() => sut.Verify(OtpCode.Create("246801"), Now));

        sut.IsVerified.ShouldBeFalse();
        sut.VerificationAttempts.ShouldBe(1);
        sut.RemainingAttempts.ShouldBe(4);
        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OtpVerificationFailedEvent>();
        evt.OtpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Purpose.ShouldBe(sut.Purpose);
        evt.AttemptNumber.ShouldBe(1);
        evt.RemainingAttempts.ShouldBe(4);
    }

    [Fact]
    public void Verify_AtFifthWrongAttempt_LocksOutAndThrowsInvalidOnThatAttempt()
    {
        var sut = new UserOtpBuilder().WithCode("135790").Build(Now);
        var wrong = OtpCode.Create("246801");

        for (var i = 0; i < 5; i++)
            Should.Throw<InvalidOtpCodeException>(() => sut.Verify(wrong, Now));

        sut.VerificationAttempts.ShouldBe(5);
        sut.RemainingAttempts.ShouldBe(0);
        sut.IsLockedOut.ShouldBeTrue();
        sut.IsUsable(Now).ShouldBeFalse();
    }

    [Fact]
    public void Verify_AfterLockout_ThrowsOtpMaxAttemptsExceededException()
    {
        var sut = new UserOtpBuilder().WithCode("135790").Build(Now);
        var wrong = OtpCode.Create("246801");
        for (var i = 0; i < 5; i++)
            Should.Throw<InvalidOtpCodeException>(() => sut.Verify(wrong, Now));

        var ex = Should.Throw<OtpMaxAttemptsExceededException>(() => sut.Verify(OtpCode.Create("135790"), Now));

        ex.OtpId.ShouldBe(sut.Id);
        ex.MaxAttempts.ShouldBe(5);
    }

    [Fact]
    public void Verify_WhenAlreadyVerified_ThrowsOtpAlreadyVerifiedException()
    {
        var code = OtpCode.Create("135790");
        var sut = new UserOtpBuilder().WithCode(code).Build(Now);
        sut.Verify(code, Now);

        var ex = Should.Throw<OtpAlreadyVerifiedException>(() => sut.Verify(code, Now));

        ex.OtpId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Verify_WhenExpired_ThrowsOtpExpiredException()
    {
        var code = OtpCode.Create("135790");
        var sut = new UserOtpBuilder().WithCode(code).WithValidity(TimeSpan.FromMinutes(5)).Build(Now);

        var expiredNow = Now.AddMinutes(6);

        sut.IsExpired(expiredNow).ShouldBeTrue();
        sut.IsUsable(expiredNow).ShouldBeFalse();
        var ex = Should.Throw<OtpExpiredException>(() => sut.Verify(code, expiredNow));
        ex.OtpId.ShouldBe(sut.Id);
    }

    [Fact]
    public void GetTimeUntilExpiry_WhenExpired_ReturnsNull()
    {
        var sut = new UserOtpBuilder().WithValidity(TimeSpan.FromMinutes(5)).Build(Now);

        sut.GetTimeUntilExpiry(Now.AddMinutes(6)).ShouldBeNull();
    }

    [Fact]
    public void MarkExpired_WhenExpiredAndNotVerified_RaisesOtpExpiredEvent()
    {
        var userId = UserId.NewId();
        var sut = new UserOtpBuilder()
            .WithUserId(userId)
            .WithPurpose(OtpPurpose.EmailVerification)
            .WithValidity(TimeSpan.FromMinutes(5))
            .Build(Now);
        var expiredNow = Now.AddMinutes(6);
        sut.ClearDomainEvents();

        sut.MarkExpired(expiredNow);

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<OtpExpiredEvent>();
        evt.OtpId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(userId);
        evt.Purpose.ShouldBe(OtpPurpose.EmailVerification);
    }

    [Fact]
    public void MarkExpired_WhenVerified_IsNoOp()
    {
        var code = OtpCode.Create("135790");
        var sut = new UserOtpBuilder().WithCode(code).Build(Now);
        sut.Verify(code, Now);
        sut.ClearDomainEvents();

        sut.MarkExpired(Now);

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkExpired_WhenNotYetExpired_IsNoOp()
    {
        var sut = new UserOtpBuilder().WithValidity(TimeSpan.FromMinutes(5)).Build(Now);
        sut.ClearDomainEvents();

        sut.MarkExpired(Now);

        sut.DomainEvents.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(OtpPurpose.EmailVerification)]
    [InlineData(OtpPurpose.PasswordReset)]
    [InlineData(OtpPurpose.PhoneVerification)]
    [InlineData(OtpPurpose.TwoFactorAuthentication)]
    [InlineData(OtpPurpose.Login)]
    public void Create_AcceptsEveryDefinedPurpose(OtpPurpose purpose)
    {
        var sut = new UserOtpBuilder().WithPurpose(purpose).Build(Now);

        sut.Purpose.ShouldBe(purpose);
    }
}
