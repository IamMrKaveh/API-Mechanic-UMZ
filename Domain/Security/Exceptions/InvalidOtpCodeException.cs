using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Security.Exceptions;

public sealed class InvalidOtpCodeException(OtpId otpId)
    : SingleValueDomainException<OtpId>(
        "INVALID_OTP_CODE",
        otpId,
        $"کد OTP وارد شده برای '{otpId}' نامعتبر است.")
{
    public OtpId OtpId => Value;
}
