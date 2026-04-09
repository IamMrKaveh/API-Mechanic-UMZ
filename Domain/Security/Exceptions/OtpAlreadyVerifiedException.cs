using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpAlreadyVerifiedException : DomainException
{
    public OtpId OtpId { get; }

    public override string ErrorCode => "OTP_ALREADY_VERIFIED";

    public OtpAlreadyVerifiedException(OtpId otpId)
        : base($"کد OTP '{otpId}' قبلاً تأیید شده است.")
    {
        OtpId = otpId;
    }
}