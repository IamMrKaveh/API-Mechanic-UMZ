using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpMaxAttemptsExceededException(OtpId otpId, int maxAttempts) : DomainException($"تعداد تلاش‌های تأیید کد OTP '{otpId}' به حداکثر ({maxAttempts}) رسیده است.")
{
    public OtpId OtpId { get; } = otpId;
    public int MaxAttempts { get; } = maxAttempts;

    public override string ErrorCode => "OTP_MAX_ATTEMPTS_EXCEEDED";
}