using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Contracts;

public interface IAuthService
{
    Task<ServiceResult> RequestOtpAsync(
        PhoneNumber phoneNumber,
        IpAddress ipAddress,
        CancellationToken ct = default);

    Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> VerifyOtpAsync(
        PhoneNumber phoneNumber,
        OtpCode code,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>> RefreshTokenAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<ServiceResult> LogoutAsync(
        UserId userId,
        RefreshToken? refreshToken,
        CancellationToken ct = default);

    Task<ServiceResult> LogoutAllAsync(
        UserId userId,
        CancellationToken ct = default);
}