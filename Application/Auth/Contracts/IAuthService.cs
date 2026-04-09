using Application.Auth.Features.Shared;
using Application.User.Features.Shared;

namespace Application.Auth.Contracts;

public interface IAuthService
{
    Task<ServiceResult> RequestOtpAsync(
        string phoneNumber,
        string ipAddress,
        CancellationToken ct = default);

    Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> VerifyOtpAsync(
        string phoneNumber,
        string code,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<ServiceResult> LogoutAsync(
        Guid userId,
        string? refreshToken,
        CancellationToken ct = default);

    Task<ServiceResult> LogoutAllAsync(
        Guid userId,
        CancellationToken ct = default);
}