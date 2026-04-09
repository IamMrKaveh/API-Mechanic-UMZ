using Application.Auth.Features.Shared;

namespace Application.Auth.Contracts;

public interface ISessionService
{
    Task<ServiceResult<RefreshTokenResult>> CreateSessionAsync(
        Guid userId,
        string ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task<ServiceResult<RefreshTokenResult>> RefreshSessionAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken ct = default);

    Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default);

    Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default);
}