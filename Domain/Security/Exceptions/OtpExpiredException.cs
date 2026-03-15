using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpExpiredException(UserOtpId otpId)
    : DomainException($"کد OTP '{otpId}' منقضی شده است.")
{
    public UserOtpId OtpId { get; } = otpId;
}