using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpExpiredException : DomainException
{
    public OtpId OtpId { get; }

    public override string ErrorCode => "OTP_EXPIRED";

    public OtpExpiredException(OtpId otpId)
        : base($"کد OTP '{otpId}' منقضی شده است.")
    {
        OtpId = otpId;
    }
}