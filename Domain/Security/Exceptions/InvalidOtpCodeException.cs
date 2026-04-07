using Domain.Common.Exceptions;
using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class InvalidOtpCodeException : DomainException
{
    public UserOtpId OtpId { get; }

    public override string ErrorCode => "INVALID_OTP_CODE";

    public InvalidOtpCodeException(UserOtpId otpId)
        : base($"کد OTP وارد شده برای '{otpId}' نامعتبر است.")
    {
        OtpId = otpId;
    }
}