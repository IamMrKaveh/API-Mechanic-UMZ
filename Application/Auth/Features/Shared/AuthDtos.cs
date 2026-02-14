namespace Application.Auth.Features.Shared;

/// <summary>
/// اینترفیس سرویس احراز هویت - حذف شد
/// تمام منطق به Command/Query Handler‌ها منتقل شده است
/// این فایل فقط شامل AuthResult است
/// </summary>
public class AuthResult
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public UserProfileDto User { get; set; } = null!;
    public bool IsNewUser { get; set; }
}

public record LoginRequestDto(string PhoneNumber);

public record VerifyOtpRequestDto(string PhoneNumber, string Code);

public record RefreshRequestDto(string refreshToken);