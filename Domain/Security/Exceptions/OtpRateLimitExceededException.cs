using Domain.Security.Enums;
using Domain.User.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpRateLimitExceededException(UserId userId, OtpPurpose purpose, TimeSpan retryAfter)
    : DomainException($"تعداد درخواست‌های OTP برای کاربر '{userId}' با هدف '{purpose}' بیش از حد مجاز است. لطفاً {retryAfter.TotalMinutes:N0} دقیقه دیگر تلاش کنید.")
{
    public UserId UserId { get; } = userId;
    public OtpPurpose Purpose { get; } = purpose;
    public TimeSpan RetryAfter { get; } = retryAfter;
}