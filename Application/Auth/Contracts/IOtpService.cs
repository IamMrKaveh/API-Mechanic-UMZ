using Domain.Security.Enums;

namespace Application.Auth.Contracts;

public interface IOtpService
{
    Task<bool> SendOtpAsync(string phoneNumber, string code, OtpPurpose purpose, CancellationToken ct = default);

    Task<bool> ValidateRateLimitAsync(Guid userId, OtpPurpose purpose, CancellationToken ct = default);
}