using Domain.Security.Aggregates;

namespace Domain.Security.Results;

public sealed class OtpVerificationResult
{
    public bool IsSuccess { get; private set; }
    public bool IsInvalidCode { get; private set; }
    public UserOtp? Otp { get; private set; }
    public string? Error { get; private set; }
    public int? RemainingAttempts { get; private set; }

    private OtpVerificationResult()
    { }

    public static OtpVerificationResult Verified(UserOtp otp) =>
        new()
        {
            IsSuccess = true,
            Otp = otp
        };

    public static OtpVerificationResult InvalidCode(int remainingAttempts) =>
        new()
        {
            IsSuccess = false,
            IsInvalidCode = true,
            RemainingAttempts = remainingAttempts,
            Error = remainingAttempts > 0
                ? $"کد وارد شده نادرست است. {remainingAttempts} تلاش باقی مانده."
                : "کد وارد شده نادرست است. تعداد تلاش‌های مجاز به پایان رسیده است."
        };

    public static OtpVerificationResult Failed(string error) =>
        new()
        {
            IsSuccess = false,
            Error = error
        };
}