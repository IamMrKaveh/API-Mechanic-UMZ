using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Interfaces;

public interface ISessionRepository
{
    Task<UserSession?> GetByIdAsync(SessionId sessionId, CancellationToken ct = default);

    Task<UserSession?> GetByRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default);

    Task AddAsync(UserSession session, CancellationToken ct = default);

    void Update(UserSession session);

    Task RevokeAllByUserIdAsync(UserId userId, CancellationToken ct = default);

    Task RevokeAllByUserIdAsync(UserId userId, SessionRevocationReason reason, CancellationToken ct = default);

    Task<UserSession?> GetActiveByUserAndDeviceAsync(UserId userId, DeviceInfo deviceInfo, CancellationToken ct = default);

    Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(DateTime cutoffTime, CancellationToken ct = default);
}