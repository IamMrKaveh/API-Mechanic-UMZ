using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.User.ValueObjects;
using SharedKernel.Results;

namespace Domain.Security.Results;

public sealed class OtpGenerationResult
{
    public bool IsSuccess { get; private set; }
    public bool IsRateLimited { get; private set; }
    public UserOtp? Otp { get; private set; }
    public string? Error { get; private set; }
    public TimeSpan? RetryAfter { get; private set; }

    private OtpGenerationResult()
    { }

    public static OtpGenerationResult Success(UserOtp otp) =>
        new()
        {
            IsSuccess = true,
            Otp = otp
        };

    public static OtpGenerationResult RateLimited(UserId userId, OtpPurpose purpose, TimeSpan retryAfter) =>
        new()
        {
            IsSuccess = false,
            IsRateLimited = true,
            RetryAfter = retryAfter,
            Error = $"تعداد درخواست‌های OTP بیش از حد مجاز است. لطفاً {retryAfter.TotalMinutes:N0} دقیقه دیگر تلاش کنید."
        };

    public static OtpGenerationResult Failed(string error) =>
        new()
        {
            IsSuccess = false,
            Error = error
        };

    public Result<UserOtp> ToResult() => IsSuccess
        ? Result<UserOtp>.Success(Otp!)
        : Result<UserOtp>.Failure(new SharedKernel.Results.Error("OtpGeneration.Failed", Error ?? string.Empty));
}