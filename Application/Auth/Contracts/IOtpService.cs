namespace Application.Auth.Contracts;

/// <summary>
/// سرویس OTP - فقط تولید و اعتبارسنجی کد
/// ذخیره‌سازی توسط Domain (User Aggregate) انجام می‌شود
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// تولید کد OTP امن
    /// </summary>
    string GenerateSecureOtp();

    /// <summary>
    /// هش کردن کد OTP
    /// </summary>
    string HashOtp(
        string otp
        );

    /// <summary>
    /// تأیید کد OTP
    /// </summary>
    bool VerifyOtp(
        string otp,
        string hash
        );
}