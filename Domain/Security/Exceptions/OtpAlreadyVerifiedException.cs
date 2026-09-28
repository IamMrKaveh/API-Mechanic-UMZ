using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Security.Exceptions;

public sealed class OtpAlreadyVerifiedException(OtpId otpId)
    : ConflictException<OtpId>(
        "OTP_ALREADY_VERIFIED",
        otpId,
        $"کد OTP '{otpId}' قبلاً تأیید شده است.")
{
    public OtpId OtpId => Value;
}
