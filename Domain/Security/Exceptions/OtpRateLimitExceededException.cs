using Domain.Common.Exceptions;
using Domain.Security.Enums;
using Domain.User.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpRateLimitExceededException : DomainException
{
    public UserId UserId { get; }
    public OtpPurpose Purpose { get; }
    public TimeSpan RetryAfter { get; }

    public override string ErrorCode => "OTP_RATE_LIMIT_EXCEEDED";

    public OtpRateLimitExceededException(UserId userId, OtpPurpose purpose, TimeSpan retryAfter)
        : base($"تعداد درخواست‌های OTP برای کاربر '{userId}' با هدف '{purpose}' بیش از حد مجاز است. لطفاً {retryAfter.TotalMinutes:N0} دقیقه دیگر تلاش کنید.")
    {
        UserId = userId;
        Purpose = purpose;
        RetryAfter = retryAfter;
    }
}