using Application.User.Features.Shared;

namespace Application.Auth.Features.Shared;

public record AuthResult
{
    public Guid AccessToken { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; init; }
    public DateTime RefreshTokenExpiresAt { get; init; }
    public UserProfileDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}

public record TokenResultDto(string AccessToken, string RefreshToken);

public record RefreshTokenResult(
    Guid SessionId,
    string RefreshToken,
    DateTime ExpiresAt,
    Guid UserId);

public sealed record SendOtpDto(
    string PhoneNumber
);

public sealed record VerifyOtpDto(
    string PhoneNumber,
    string Code,
    string? DeviceInfo = null
);

public sealed record RefreshTokenDto(
    string RefreshToken
);

public sealed record RevokeSessionDto(
    Guid SessionId
);