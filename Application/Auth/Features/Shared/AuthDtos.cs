using Application.User.Features.Shared;

namespace Application.Auth.Features.Shared;

public record AuthResult
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; init; }

    public DateTime RefreshTokenExpiresAt { get; init; }

    public UserProfileDto User { get; init; } = null!;

    public bool IsNewUser { get; init; }
}

public record UserSessionDto
{
    public Guid Id { get; init; }
    public string SessionType { get; init; } = string.Empty;
    public string CreatedByIp { get; init; } = string.Empty;
    public string? DeviceInfo { get; init; }
    public string? BrowserInfo { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCurrent { get; init; }
}

public record TokenResultDto(string AccessToken, string RefreshToken);

public record RefreshTokenResult(
    Guid SessionId,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId);