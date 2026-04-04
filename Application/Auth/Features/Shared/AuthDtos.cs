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