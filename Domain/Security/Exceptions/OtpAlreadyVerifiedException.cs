namespace Domain.Security.Exceptions;

public sealed class OtpAlreadyVerifiedException(UserOtpId otpId)
    : DomainException($"کد OTP '{otpId}' قبلاً تأیید شده است.")
{
    public UserOtpId OtpId { get; } = otpId;
}