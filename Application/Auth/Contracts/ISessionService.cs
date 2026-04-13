using Application.Auth.Features.Shared;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Auth.Contracts;

public interface ISessionService
{
    Task<ServiceResult<RefreshTokenResult>> CreateSessionAsync(
        UserId userId,
        IpAddress ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<ServiceResult<RefreshTokenResult>> RefreshSessionAsync(
        RefreshToken refreshToken,
        IpAddress ipAddress,
        CancellationToken ct = default);

    Task RevokeSessionAsync(
        SessionId sessionId,
        CancellationToken ct = default);

    Task RevokeAllSessionsAsync(
        UserId userId,
        CancellationToken ct = default);
}