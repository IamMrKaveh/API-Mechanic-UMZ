using Domain.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Interfaces;

public interface ISessionRepository : IRepository<UserSession, SessionId>
{
    Task<UserSession?> GetByRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default);

    Task RevokeAllByUserIdAsync(UserId userId, CancellationToken ct = default);

    Task RevokeAllByUserIdAsync(UserId userId, SessionRevocationReason reason, CancellationToken ct = default);

    Task RevokeAllExceptAsync(UserId userId, SessionId exceptSessionId, SessionRevocationReason reason, CancellationToken ct = default);

    Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default);

    Task<UserSession?> GetActiveByUserAndDeviceAsync(UserId userId, DeviceInfo deviceInfo, CancellationToken ct = default);

    Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(DateTime cutoffTime, CancellationToken ct = default);
}
