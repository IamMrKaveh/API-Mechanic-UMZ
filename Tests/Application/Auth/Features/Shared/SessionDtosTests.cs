using Application.Auth.Features.Shared;

namespace Tests.Application.Auth.Features.Shared;

public class SessionDtosTests
{
    [Fact]
    public void UserSessionDto_Defaults_AreEmpty()
    {
        var dto = new UserSessionDto();

        dto.Id.ShouldBe(default(Guid));
        dto.CreatedByIp.ShouldBe(string.Empty);
        dto.DeviceInfo.ShouldBe(string.Empty);
        dto.CreatedAt.ShouldBe(default);
        dto.LastActivityAt.ShouldBeNull();
        dto.ExpiresAt.ShouldBe(default);
        dto.SessionType.ShouldBeNull();
        dto.BrowserInfo.ShouldBeNull();
        dto.PlatformInfo.ShouldBeNull();
        dto.IsCurrent.ShouldBeFalse();
        dto.RemainingSeconds.ShouldBe(0);
        dto.IsExpiringSoon.ShouldBeFalse();
    }

    [Fact]
    public void UserSessionDto_InitProperties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-1);
        var dto = new UserSessionDto
        {
            Id = id,
            CreatedByIp = "1.2.3.4",
            DeviceInfo = "device",
            CreatedAt = createdAt,
            LastActivityAt = createdAt.AddMinutes(10),
            ExpiresAt = createdAt.AddDays(7),
            SessionType = "Refresh",
            BrowserInfo = "Chrome",
            PlatformInfo = "Windows",
            IsCurrent = true,
            RemainingSeconds = 600_000,
            IsExpiringSoon = false
        };

        dto.Id.ShouldBe(id);
        dto.IsCurrent.ShouldBeTrue();
        dto.RemainingSeconds.ShouldBe(600_000);
        dto.SessionType.ShouldBe("Refresh");
    }

    [Fact]
    public void CurrentSessionDto_Defaults_AreUnauthenticated()
    {
        var dto = new CurrentSessionDto();

        dto.SessionId.ShouldBeNull();
        dto.UserId.ShouldBeNull();
        dto.IpAddress.ShouldBeNull();
        dto.UserAgent.ShouldBeNull();
        dto.IsAuthenticated.ShouldBeFalse();
        dto.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void CurrentSessionDto_AuthenticatedSession_RoundTrip()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dto = new CurrentSessionDto
        {
            SessionId = sessionId,
            UserId = userId,
            IpAddress = "5.6.7.8",
            UserAgent = "ua",
            IsAuthenticated = true,
            IsAdmin = true
        };

        dto.SessionId.ShouldBe(sessionId);
        dto.UserId.ShouldBe(userId);
        dto.IsAuthenticated.ShouldBeTrue();
        dto.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public void UserSessionDto_WithExpression_PreservesOthers()
    {
        var dto = new UserSessionDto { Id = Guid.NewGuid(), CreatedByIp = "1.1.1.1", IsCurrent = false };

        var updated = dto with { IsCurrent = true };

        updated.IsCurrent.ShouldBeTrue();
        updated.CreatedByIp.ShouldBe("1.1.1.1");
    }
}
