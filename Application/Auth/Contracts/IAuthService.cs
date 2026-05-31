using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Security.ValueObjects;

namespace Application.Auth.Contracts;

public interface IAuthService
{
    Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> RefreshTokenAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default);
}