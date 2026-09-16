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

public record AuthResultResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    UserProfileDto User,
    bool IsNewUser)
{
    public static AuthResultResponse FromAuthResult(AuthResult result) => new(
        result.AccessToken,
        result.AccessTokenExpiresAt,
        result.RefreshTokenExpiresAt,
        result.User,
        result.IsNewUser);
}

public record TokenResultDto(string AccessToken, string RefreshToken);

public record RefreshTokenResult(
    Guid SessionId,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId);
