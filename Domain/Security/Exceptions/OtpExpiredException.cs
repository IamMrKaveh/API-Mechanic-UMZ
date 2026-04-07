using Domain.Common.Exceptions;
using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpExpiredException : DomainException
{
    public UserOtpId OtpId { get; }

    public override string ErrorCode => "OTP_EXPIRED";

    public OtpExpiredException(UserOtpId otpId)
        : base($"کد OTP '{otpId}' منقضی شده است.")
    {
        OtpId = otpId;
    }
}