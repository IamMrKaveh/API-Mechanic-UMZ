using Domain.Security.Enums;

namespace Application.Auth.Contracts;

public interface IOtpService
{
    string HashOtp(string otp);

    Task<bool> SendOtpAsync(string phoneNumber, string code, OtpPurpose purpose, CancellationToken ct = default);

    Task<bool> ValidateRateLimitAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);
}