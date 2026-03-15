using Domain.Security.Aggregates;

namespace Domain.Security.Rules;

public sealed class OtpMustBeUsableRule : IBusinessRule
{
    private readonly UserOtp _otp;

    public OtpMustBeUsableRule(UserOtp otp)
    {
        _otp = otp;
    }

    public bool IsBroken()
    {
        return !_otp.IsUsable;
    }

    public string Message
    {
        get
        {
            if (_otp.IsVerified) return "کد OTP قبلاً استفاده شده است.";
            if (_otp.IsExpired) return "کد OTP منقضی شده است.";
            if (_otp.IsLockedOut) return "تعداد تلاش‌های مجاز به پایان رسیده است.";
            return "کد OTP قابل استفاده نیست.";
        }
    }
}