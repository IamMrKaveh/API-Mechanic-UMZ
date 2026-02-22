namespace Application.Auth.Contracts;

/// <summary>
/// سرویس تولید و مدیریت توکن
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// تولید JWT Token
    /// </summary>
    string GenerateJwtToken(
        Domain.User.User user
        );

    /// <summary>
    /// تولید Refresh Token
    /// </summary>
    RefreshTokenResult GenerateRefreshToken();

    /// <summary>
    /// هش کردن Refresh Token
    /// </summary>
    string HashToken(
        string token
        );

    /// <summary>
    /// استخراج اطلاعات از JWT (بدون اعتبارسنجی امضا)
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(
        string token
        );

    /// <summary>
    /// تجزیه Refresh Token به Selector و Verifier
    /// </summary>
    (string? Selector, string? Verifier) ParseRefreshToken(
        string refreshToken
        );

    /// <summary>
    /// دریافت زمان انقضای Access Token
    /// </summary>
    DateTime GetAccessTokenExpiration();

    /// <summary>
    /// دریافت زمان انقضای Refresh Token
    /// </summary>
    DateTime GetRefreshTokenExpiration();
}

public class RefreshTokenResult
{
    public string Selector { get; set; } = null!;
    public string Verifier { get; set; } = null!;
    public string FullToken { get; set; } = null!;
}