using Application.Auth.Contracts;
using Application.Auth.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Options;
using Infrastructure.Security.Options;
using Infrastructure.Security.Settings;

namespace Infrastructure.Auth.Services;

public sealed class SessionService(
    ISessionRepository sessionRepository,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthOptions> authOptions,
    IUnitOfWork unitOfWork) : ISessionService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly AuthOptions _authOptions = authOptions.Value;

    public async Task<ServiceResult<RefreshTokenResult>> CreateSessionAsync(
        UserId userId,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var selector = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hashedVerifier = HashVerifier(verifier);

        var expiresAt = DateTime.UtcNow.AddDays(_authOptions.SessionExpirationDays);

        var session = UserSession.Create(
            SessionId.NewId(),
            userId,
            selector,
            hashedVerifier,
            ipAddress.Value,
            userAgent,
            expiresAt);

        await sessionRepository.AddAsync(session, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var token = $"{selector}.{verifier}";

        return ServiceResult<RefreshTokenResult>.Success(
            new RefreshTokenResult(session.Id.Value, token, expiresAt, userId.Value));
    }

    public async Task<ServiceResult<RefreshTokenResult>> RefreshSessionAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        CancellationToken ct = default)
    {
        var parts = refreshToken.Value.Split('.');
        if (parts.Length != 2)
            return ServiceResult<RefreshTokenResult>.Unauthorized("توکن نامعتبر است.");

        var selector = parts[0];
        var verifier = parts[1];

        var session = await sessionRepository.GetBySelectorAsync(selector, ct);

        if (session is null || session.IsRevoked || session.ExpiresAt < DateTime.UtcNow)
            return ServiceResult<RefreshTokenResult>.Unauthorized("جلسه نامعتبر یا منقضی است.");

        if (!VerifyToken(verifier, session.HashedVerifier))
            return ServiceResult<RefreshTokenResult>.Unauthorized("توکن نامعتبر است.");

        var newVerifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hashedNewVerifier = HashVerifier(newVerifier);
        var expiresAt = DateTime.UtcNow.AddDays(_authOptions.SessionExpirationDays);

        session.Refresh(hashedNewVerifier, expiresAt, ipAddress.Value);
        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        var newToken = $"{selector}.{newVerifier}";
        return ServiceResult<RefreshTokenResult>.Success(
            new RefreshTokenResult(session.Id.Value, newToken, expiresAt, session.UserId.Value));
    }

    public async Task RevokeSessionAsync(SessionId sessionId, CancellationToken ct = default)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is null) return;

        session.Revoke(SessionRevocationReason.UserLogout);
        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RevokeAllSessionsAsync(UserId userId, CancellationToken ct = default)
    {
        await sessionRepository.RevokeAllByUserIdAsync(userId, SessionRevocationReason.AdminAction, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static string HashVerifier(string verifier)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(verifier);
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    private static bool VerifyToken(string verifier, string hashedVerifier)
    {
        var hashed = HashVerifier(verifier);
        return string.Equals(hashed, hashedVerifier, StringComparison.Ordinal);
    }
}