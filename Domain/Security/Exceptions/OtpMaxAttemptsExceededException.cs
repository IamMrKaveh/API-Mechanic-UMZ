using Domain.Common.Exceptions;
using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpMaxAttemptsExceededException : DomainException
{
    public OtpId OtpId { get; }
    public int MaxAttempts { get; }

    public override string ErrorCode => "OTP_MAX_ATTEMPTS_EXCEEDED";

    public OtpMaxAttemptsExceededException(OtpId otpId, int maxAttempts)
        : base($"تعداد تلاش‌های تأیید کد OTP '{otpId}' به حداکثر ({maxAttempts}) رسیده است.")
    {
        OtpId = otpId;
        MaxAttempts = maxAttempts;
    }
}