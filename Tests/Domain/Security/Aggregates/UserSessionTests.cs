using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Events;
using Domain.Security.Exceptions;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Security.Aggregates;

public class UserSessionTests
{
    [Fact]
    public void Create_WithValidInput_InitializesAllStateFields()
    {
        var id = SessionId.NewId();
        var userId = UserId.NewId();
        var refreshToken = RefreshToken.Generate();
        var deviceInfo = DeviceInfo.Create("Chrome 120");
        var ip = IpAddress.Create("192.168.1.10");
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new UserSessionBuilder()
            .WithId(id)
            .WithUserId(userId)
            .WithRefreshToken(refreshToken)
            .WithDeviceInfo(deviceInfo)
            .WithIpAddress(ip)
            .WithExpiresAt(expiresAt)
            .Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.Id.ShouldBe(id);
        sut.UserId.ShouldBe(userId);
        sut.RefreshToken.ShouldBe(refreshToken);
        sut.DeviceInfo.ShouldBe(deviceInfo);
        sut.IpAddress.ShouldBe(ip);
        sut.ExpiresAt.ShouldBe(expiresAt);
        sut.IsRevoked.ShouldBeFalse();
        sut.RevocationReason.ShouldBeNull();
        sut.RevokedAt.ShouldBeNull();
        sut.IsExpired.ShouldBeFalse();
        sut.IsActive.ShouldBeTrue();
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
        sut.LastActivityAt.ShouldNotBeNull();
        sut.LastActivityAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Fact]
    public void Create_RaisesExactlyOneSessionCreatedEventWithMatchingFields()
    {
        var id = SessionId.NewId();
        var userId = UserId.NewId();
        var deviceInfo = DeviceInfo.Create("Firefox 121");
        var ip = IpAddress.Create("10.0.0.5");
        var expiresAt = DateTime.UtcNow.AddDays(3);

        var sut = new UserSessionBuilder()
            .WithId(id)
            .WithUserId(userId)
            .WithDeviceInfo(deviceInfo)
            .WithIpAddress(ip)
            .WithExpiresAt(expiresAt)
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<SessionCreatedEvent>();
        evt.SessionId.ShouldBe(id);
        evt.UserId.ShouldBe(userId);
        evt.DeviceInfo.ShouldBe(deviceInfo);
        evt.IpAddress.ShouldBe(ip);
        evt.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void Create_WithNullId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserSession.Create(null!, UserId.NewId(), RefreshToken.Generate(),
                DeviceInfo.Unknown, IpAddress.Unknown, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithNullUserId_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserSession.Create(SessionId.NewId(), null!, RefreshToken.Generate(),
                DeviceInfo.Unknown, IpAddress.Unknown, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithNullRefreshToken_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserSession.Create(SessionId.NewId(), UserId.NewId(), null!,
                DeviceInfo.Unknown, IpAddress.Unknown, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithNullDeviceInfo_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserSession.Create(SessionId.NewId(), UserId.NewId(), RefreshToken.Generate(),
                null!, IpAddress.Unknown, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithNullIpAddress_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() =>
            UserSession.Create(SessionId.NewId(), UserId.NewId(), RefreshToken.Generate(),
                DeviceInfo.Unknown, null!, DateTime.UtcNow.AddDays(1)));
    }

    [Fact]
    public void Create_WithExpiresAtInPast_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            new UserSessionBuilder().WithExpiresAt(DateTime.UtcNow.AddMinutes(-1)).Build());
    }

    [Fact]
    public void Create_WithExpiresAtEqualToUtcNow_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            UserSession.Create(SessionId.NewId(), UserId.NewId(), RefreshToken.Generate(),
                DeviceInfo.Unknown, IpAddress.Unknown, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithExpiresAtAboveNinetyDays_ThrowsDomainException()
    {
        Should.Throw<DomainException>(() =>
            new UserSessionBuilder().WithExpiresAt(DateTime.UtcNow.AddDays(91)).Build());
    }

    [Fact]
    public void Create_WithExpiresAtAtEightyNineDays_Succeeds()
    {
        Should.NotThrow(() => new UserSessionBuilder().WithExpiresAt(DateTime.UtcNow.AddDays(89)).Build());
    }

    [Fact]
    public void Revoke_OnActiveSession_SetsRevokedStateAndRaisesSessionRevokedEvent()
    {
        var sut = new UserSessionBuilder().Build();
        sut.ClearDomainEvents();

        sut.Revoke(SessionRevocationReason.PasswordChanged);

        sut.IsRevoked.ShouldBeTrue();
        sut.IsActive.ShouldBeFalse();
        sut.RevokedAt.ShouldNotBeNull();
        sut.RevocationReason.ShouldBe(SessionRevocationReason.PasswordChanged);
        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<SessionRevokedEvent>();
        evt.SessionId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
        evt.Reason.ShouldBe(SessionRevocationReason.PasswordChanged);
    }

    [Fact]
    public void Revoke_DefaultReason_IsUserRequested()
    {
        var sut = new UserSessionBuilder().Build();
        sut.ClearDomainEvents();

        sut.Revoke();

        sut.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        sut.DomainEvents.Single().ShouldBeOfType<SessionRevokedEvent>()
            .Reason.ShouldBe(SessionRevocationReason.UserRequested);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_IsNoOp()
    {
        var sut = new UserSessionBuilder().Build();
        sut.Revoke(SessionRevocationReason.UserRequested);
        var revokedAt = sut.RevokedAt;
        sut.ClearDomainEvents();

        sut.Revoke(SessionRevocationReason.AdminRevoked);

        sut.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        sut.RevokedAt.ShouldBe(revokedAt);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task Revoke_WhenExpired_ThrowsSessionExpiredException()
    {
        var sut = new UserSessionBuilder().WithExpiresAt(DateTime.UtcNow.AddMilliseconds(10)).Build();
        await Task.Delay(100);

        var ex = Should.Throw<SessionExpiredException>(() => sut.Revoke());

        ex.SessionId.ShouldBe(sut.Id);
    }

    [Theory]
    [InlineData(SessionRevocationReason.UserRequested)]
    [InlineData(SessionRevocationReason.AdminRevoked)]
    [InlineData(SessionRevocationReason.SecurityConcern)]
    [InlineData(SessionRevocationReason.PasswordChanged)]
    [InlineData(SessionRevocationReason.AccountDeactivated)]
    [InlineData(SessionRevocationReason.AllSessionsRevoked)]
    [InlineData(SessionRevocationReason.PhoneChanged)]
    public void Revoke_AcceptsAnyReasonAndPropagatesItIntoEvent(SessionRevocationReason reason)
    {
        var sut = new UserSessionBuilder().Build();
        sut.ClearDomainEvents();

        sut.Revoke(reason);

        sut.RevocationReason.ShouldBe(reason);
        sut.DomainEvents.Single().ShouldBeOfType<SessionRevokedEvent>().Reason.ShouldBe(reason);
    }

    [Fact]
    public void MarkExpired_OnActiveSession_MarksRevokedAsExpiredAndRaisesSessionExpiredEvent()
    {
        var sut = new UserSessionBuilder().Build();
        sut.ClearDomainEvents();

        sut.MarkExpired();

        sut.IsRevoked.ShouldBeTrue();
        sut.RevocationReason.ShouldBe(SessionRevocationReason.Expired);
        sut.RevokedAt.ShouldNotBeNull();
        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<SessionExpiredEvent>();
        evt.SessionId.ShouldBe(sut.Id);
        evt.UserId.ShouldBe(sut.UserId);
    }

    [Fact]
    public void MarkExpired_WhenAlreadyRevoked_IsNoOp()
    {
        var sut = new UserSessionBuilder().Build();
        sut.Revoke(SessionRevocationReason.UserRequested);
        sut.ClearDomainEvents();

        sut.MarkExpired();

        sut.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateActivity_WithNewerTimestamp_UpdatesLastActivityAt()
    {
        var sut = new UserSessionBuilder().Build();
        var newer = DateTime.UtcNow.AddHours(1);

        sut.UpdateActivity(newer);

        sut.LastActivityAt.ShouldBe(newer);
    }

    [Fact]
    public void UpdateActivity_WithOlderOrEqualTimestamp_IsNoOp()
    {
        var sut = new UserSessionBuilder().Build();
        var original = sut.LastActivityAt!.Value;

        sut.UpdateActivity(original);
        sut.UpdateActivity(original.AddSeconds(-1));

        sut.LastActivityAt.ShouldBe(original);
    }

    [Fact]
    public void UpdateActivity_OnRevokedSession_IsNoOp()
    {
        var sut = new UserSessionBuilder().Build();
        sut.Revoke(SessionRevocationReason.UserRequested);
        var last = sut.LastActivityAt;

        sut.UpdateActivity(DateTime.UtcNow.AddHours(1));

        sut.LastActivityAt.ShouldBe(last);
    }

    [Fact]
    public async Task UpdateActivity_OnExpiredSession_IsNoOp()
    {
        var sut = new UserSessionBuilder().WithExpiresAt(DateTime.UtcNow.AddMilliseconds(10)).Build();
        await Task.Delay(100);
        var last = sut.LastActivityAt;

        sut.UpdateActivity(DateTime.UtcNow.AddHours(1));

        sut.LastActivityAt.ShouldBe(last);
    }

    [Fact]
    public void ValidateRefreshToken_OnActiveSessionWithMatchingToken_ReturnsTrue()
    {
        var payload = new string('a', 64);
        var token = RefreshToken.Create(payload);
        var sut = new UserSessionBuilder().WithRefreshToken(token).Build();

        sut.ValidateRefreshToken(payload).ShouldBeTrue();
    }

    [Fact]
    public void ValidateRefreshToken_OnActiveSessionWithDifferentToken_ReturnsFalse()
    {
        var sut = new UserSessionBuilder().WithRefreshToken(RefreshToken.Create(new string('a', 64))).Build();

        sut.ValidateRefreshToken(new string('b', 64)).ShouldBeFalse();
    }

    [Fact]
    public void ValidateRefreshToken_OnRevokedSession_ReturnsFalseEvenForMatchingToken()
    {
        var payload = new string('a', 64);
        var sut = new UserSessionBuilder().WithRefreshToken(RefreshToken.Create(payload)).Build();
        sut.Revoke(SessionRevocationReason.UserRequested);

        sut.ValidateRefreshToken(payload).ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateRefreshToken_OnExpiredSession_ReturnsFalseEvenForMatchingToken()
    {
        var payload = new string('a', 64);
        var sut = new UserSessionBuilder()
            .WithRefreshToken(RefreshToken.Create(payload))
            .WithExpiresAt(DateTime.UtcNow.AddMilliseconds(10))
            .Build();
        await Task.Delay(100);

        sut.ValidateRefreshToken(payload).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRefreshToken_OnActiveSessionWithNullOrWhitespaceProvided_ReturnsFalse(string? provided)
    {
        var sut = new UserSessionBuilder().Build();

        sut.ValidateRefreshToken(provided!).ShouldBeFalse();
    }
}
