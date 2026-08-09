using Domain.Security.Enums;
using Domain.Security.Events;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.Domain.Security.Events;

public class SecurityEventsTests
{
    [Fact]
    public void OtpGeneratedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var otpId = OtpId.NewId();
        var userId = UserId.NewId();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        var sut = new OtpGeneratedEvent(otpId, userId, OtpPurpose.Login, expiresAt);

        sut.OtpId.ShouldBe(otpId);
        sut.UserId.ShouldBe(userId);
        sut.Purpose.ShouldBe(OtpPurpose.Login);
        sut.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void OtpVerifiedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var otpId = OtpId.NewId();
        var userId = UserId.NewId();

        var sut = new OtpVerifiedEvent(otpId, userId, OtpPurpose.PasswordReset);

        sut.OtpId.ShouldBe(otpId);
        sut.UserId.ShouldBe(userId);
        sut.Purpose.ShouldBe(OtpPurpose.PasswordReset);
    }

    [Fact]
    public void OtpVerificationFailedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var otpId = OtpId.NewId();
        var userId = UserId.NewId();

        var sut = new OtpVerificationFailedEvent(otpId, userId, OtpPurpose.TwoFactorAuthentication, 3, 2);

        sut.OtpId.ShouldBe(otpId);
        sut.UserId.ShouldBe(userId);
        sut.Purpose.ShouldBe(OtpPurpose.TwoFactorAuthentication);
        sut.AttemptNumber.ShouldBe(3);
        sut.RemainingAttempts.ShouldBe(2);
    }

    [Fact]
    public void OtpExpiredEvent_ExposesConstructorArgumentsAsProperties()
    {
        var otpId = OtpId.NewId();
        var userId = UserId.NewId();

        var sut = new OtpExpiredEvent(otpId, userId, OtpPurpose.EmailVerification);

        sut.OtpId.ShouldBe(otpId);
        sut.UserId.ShouldBe(userId);
        sut.Purpose.ShouldBe(OtpPurpose.EmailVerification);
    }

    [Fact]
    public void SessionCreatedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var sessionId = SessionId.NewId();
        var userId = UserId.NewId();
        var deviceInfo = DeviceInfo.Create("Chrome 120");
        var ip = IpAddress.Create("192.168.1.1");
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var sut = new SessionCreatedEvent(sessionId, userId, deviceInfo, ip, expiresAt);

        sut.SessionId.ShouldBe(sessionId);
        sut.UserId.ShouldBe(userId);
        sut.DeviceInfo.ShouldBe(deviceInfo);
        sut.IpAddress.ShouldBe(ip);
        sut.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void SessionRevokedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var sessionId = SessionId.NewId();
        var userId = UserId.NewId();

        var sut = new SessionRevokedEvent(sessionId, userId, SessionRevocationReason.PasswordChanged);

        sut.SessionId.ShouldBe(sessionId);
        sut.UserId.ShouldBe(userId);
        sut.Reason.ShouldBe(SessionRevocationReason.PasswordChanged);
    }

    [Fact]
    public void SessionExpiredEvent_ExposesConstructorArgumentsAsProperties()
    {
        var sessionId = SessionId.NewId();
        var userId = UserId.NewId();

        var sut = new SessionExpiredEvent(sessionId, userId);

        sut.SessionId.ShouldBe(sessionId);
        sut.UserId.ShouldBe(userId);
    }

    [Fact]
    public void AllSessionsRevokedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var userId = UserId.NewId();

        var sut = new AllSessionsRevokedEvent(userId, SessionRevocationReason.AllSessionsRevoked, 4);

        sut.UserId.ShouldBe(userId);
        sut.Reason.ShouldBe(SessionRevocationReason.AllSessionsRevoked);
        sut.RevokedCount.ShouldBe(4);
    }

    [Fact]
    public void UserLoggedInEvent_ExposesConstructorArgumentsAsProperties()
    {
        var userId = UserId.NewId();

        new UserLoggedInEvent(userId).UserId.ShouldBe(userId);
    }

    [Fact]
    public void UserLoginFailedEvent_ExposesConstructorArgumentsAsProperties()
    {
        var userId = UserId.NewId();

        var sut = new UserLoginFailedEvent(userId, 3);

        sut.UserId.ShouldBe(userId);
        sut.FailedAttempts.ShouldBe(3);
    }

    [Fact]
    public void UserLockedOutEvent_ExposesConstructorArgumentsAsProperties()
    {
        var userId = UserId.NewId();
        var lockoutEnd = DateTime.UtcNow.AddMinutes(30);

        var sut = new UserLockedOutEvent(userId, lockoutEnd, 5);

        sut.UserId.ShouldBe(userId);
        sut.LockoutEnd.ShouldBe(lockoutEnd);
        sut.FailedAttempts.ShouldBe(5);
    }
}
