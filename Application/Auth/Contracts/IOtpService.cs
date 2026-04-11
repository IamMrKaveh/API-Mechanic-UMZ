using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Contracts;

public interface IOtpService
{
    string HashOtp(OtpCode otp);

    Task<bool> SendOtpAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        OtpPurpose purpose,
        CancellationToken ct = default);

    Task<bool> ValidateRateLimitAsync(
        UserId userId,
        OtpPurpose purpose,
        CancellationToken ct = default);

    bool VerifyOtpAsync(string otpCode, CancellationToken ct);
}