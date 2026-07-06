using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.Interfaces;
using Mapster;

namespace Infrastructure.Auth.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ISessionService sessionService,
    IJwtTokenGenerator jwtTokenGenerator) : IAuthService
{
    public async Task<ServiceResult<(string AccessToken, RefreshTokenResult RefreshToken, UserProfileDto User, bool IsNewUser)>>
        RefreshTokenAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var existingSession = await sessionRepository.GetByRefreshTokenAsync(refreshToken, ct);
        if (existingSession is null || !existingSession.IsActive)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized("جلسه نامعتبر است.");

        var userId = existingSession.UserId;

        var sessionResult = await sessionService.RefreshSessionAsync(refreshToken, ipAddress, ct);
        if (!sessionResult.IsSuccess)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized(sessionResult.Error!);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.NotFound("کاربر یافت نشد.");

        if (!user.IsActive)
            return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Unauthorized("حساب کاربری غیرفعال است.");

        var newSessionId = SessionId.From(sessionResult.Value!.SessionId);
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user, newSessionId);
        var userDto = user.Adapt<UserProfileDto>();

        return ServiceResult<(string, RefreshTokenResult, UserProfileDto, bool)>.Success(
            (accessToken, sessionResult.Value!, userDto, false));
    }
}