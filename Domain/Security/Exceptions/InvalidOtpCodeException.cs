using Domain.Security.ValueObjects;

namespace Domain.Security.Exceptions;

public sealed class InvalidOtpCodeException(UserOtpId otpId)
    : DomainException($"کد OTP وارد شده برای '{otpId}' نامعتبر است.")
{
    public UserOtpId OtpId { get; } = otpId;
}