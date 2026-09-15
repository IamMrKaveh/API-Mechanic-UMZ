using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Services;

public sealed class SessionService(
    ISessionRepository sessionRepository,
    IOptions<AuthOptions> authOptions,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ISessionService
{
    private readonly AuthOptions _authOptions = authOptions.Value;

    public async Task<ServiceResult<RefreshTokenResult>> CreateSessionAsync(
        UserId userId,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var token = RefreshToken.Generate();
        var deviceInfo = DeviceInfo.Create(userAgent ?? "Unknown");
        var now = dateTimeProvider.UtcNow;
        var expiresAt = now.AddDays(_authOptions.SessionExpirationDays);

        var existingSession = await sessionRepository
            .GetActiveByUserAndDeviceAsync(userId, deviceInfo, ct);

        if (existingSession is not null)
        {
            existingSession.Revoke(now, SessionRevocationReason.UserRequested);
            sessionRepository.Update(existingSession);
        }

        var session = UserSession.Create(
            SessionId.NewId(),
            userId,
            token,
            deviceInfo,
            ipAddress,
            expiresAt,
            now);

        await sessionRepository.AddAsync(session, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<RefreshTokenResult>.Success(
            new RefreshTokenResult(
                session.Id.Value,
                token.Value,
                expiresAt,
                session.UserId.Value));
    }

    public async Task<ServiceResult<RefreshTokenResult>> RefreshSessionAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        CancellationToken ct = default)
    {
        var session = await sessionRepository.GetByRefreshTokenAsync(refreshToken, ct);

        var now = dateTimeProvider.UtcNow;
        if (session is null || !session.IsActive(now))
            return ServiceResult<RefreshTokenResult>.Unauthorized("جلسه نامعتبر یا منقضی است.");

        if (!session.ValidateRefreshToken(refreshToken.Value, now))
            return ServiceResult<RefreshTokenResult>.Unauthorized("توکن نامعتبر است.");

        session.Revoke(now, SessionRevocationReason.UserRequested);
        sessionRepository.Update(session);

        var newToken = RefreshToken.Generate();
        var deviceInfo = session.DeviceInfo;
        var expiresAt = now.AddDays(_authOptions.SessionExpirationDays);

        var newSession = UserSession.Create(
            SessionId.NewId(),
            session.UserId,
            newToken,
            deviceInfo,
            ipAddress,
            expiresAt,
            now);

        await sessionRepository.AddAsync(newSession, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<RefreshTokenResult>.Success(new RefreshTokenResult(
            newSession.Id.Value,
            newToken.Value,
            expiresAt,
            newSession.UserId.Value));
    }

    public async Task RevokeAllSessionsAsync(UserId userId, CancellationToken ct = default)
    {
        await sessionRepository.RevokeAllByUserIdAsync(userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}