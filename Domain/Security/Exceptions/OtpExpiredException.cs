using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpExpiredException(OtpId otpId) : DomainException($"کد OTP '{otpId}' منقضی شده است.")
{
    public OtpId OtpId { get; } = otpId;

    public override string ErrorCode => "OTP_EXPIRED";
}