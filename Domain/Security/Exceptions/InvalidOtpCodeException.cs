using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class InvalidOtpCodeException(OtpId otpId) : DomainException($"کد OTP وارد شده برای '{otpId}' نامعتبر است.")
{
    public OtpId OtpId { get; } = otpId;

    public override string ErrorCode => "INVALID_OTP_CODE";
}