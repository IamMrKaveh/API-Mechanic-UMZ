namespace Application.Auth.Features.Shared;

/// <summary>
/// اینترفیس سرویس احراز هویت - حذف شد
/// تمام منطق به Command/Query Handler‌ها منتقل شده است
/// این فایل فقط شامل AuthResult است
/// </summary>
public class AuthResult
{
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public DateTime AccessTokenExpiresAt { get; init; }
    public DateTime RefreshTokenExpiresAt { get; init; }
    public UserProfileDto User { get; init; } = null!;
    public bool IsNewUser { get; init; }
}

public record LoginRequestDto(string PhoneNumber);

public record VerifyOtpRequestDto(string PhoneNumber, string Code);

public record RefreshRequestDto(string refreshToken);