using Domain.Security.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Security.Exceptions;

public sealed class OtpExpiredException(OtpId otpId)
    : SingleValueDomainException<OtpId>(
        "OTP_EXPIRED",
        otpId,
        $"کد OTP '{otpId}' منقضی شده است.")
{
    public OtpId OtpId => Value;
}
