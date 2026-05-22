using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class OtpAlreadyVerifiedException(OtpId otpId) : DomainException($"کد OTP '{otpId}' قبلاً تأیید شده است.")
{
    public OtpId OtpId { get; } = otpId;

    public override string ErrorCode => "OTP_ALREADY_VERIFIED";
}