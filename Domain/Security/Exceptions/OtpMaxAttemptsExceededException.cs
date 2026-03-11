namespace Domain.Security.Exceptions;

public sealed class OtpMaxAttemptsExceededException(UserOtpId otpId, int maxAttempts)
    : DomainException($"تعداد تلاش‌های تأیید کد OTP '{otpId}' به حداکثر ({maxAttempts}) رسیده است.")
{
    public UserOtpId OtpId { get; } = otpId;
    public int MaxAttempts { get; } = maxAttempts;
}