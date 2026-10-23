using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Auth.Repositories;

public sealed class SessionRepository(DBContext context, IDateTimeProvider dateTimeProvider)
    : RepositoryBase<UserSession, SessionId>(context), ISessionRepository
{
    public async Task<UserSession?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default)
    {
        return await Context.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash, ct);
    }

    public Task RevokeAllByUserIdAsync(UserId userId, CancellationToken ct = default)
        => RevokeAllByUserIdAsync(userId, SessionRevocationReason.AllSessionsRevoked, ct);

    public async Task RevokeAllByUserIdAsync(UserId userId, SessionRevocationReason reason, CancellationToken ct = default)
    {
        // Set-based update: revokes every active session in a single statement
        // instead of loading entities and issuing one UPDATE per row.
        await Context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.IsRevoked, true)
                .SetProperty(s => s.RevokedAt, dateTimeProvider.UtcNow)
                .SetProperty(s => s.RevocationReason, reason), ct);
    }

    public async Task RevokeAllExceptAsync(UserId userId, SessionId exceptSessionId, SessionRevocationReason reason, CancellationToken ct = default)
    {
        await Context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.Id != exceptSessionId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.IsRevoked, true)
                .SetProperty(s => s.RevokedAt, dateTimeProvider.UtcNow)
                .SetProperty(s => s.RevocationReason, reason), ct);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        var now = dateTimeProvider.UtcNow;
        var results = await Context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<UserSession?> GetActiveByUserAndDeviceAsync(UserId userId, DeviceInfo deviceInfo, CancellationToken ct = default)
    {
        return await Context.UserSessions
            .FirstOrDefaultAsync(
                s => s.UserId == userId
                  && s.DeviceInfo == deviceInfo
                  && !s.IsRevoked,
                ct);
    }

    public async Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(DateTime cutoffTime, CancellationToken ct = default)
    {
        // Bounded batch: the expired set grows without bound, so the cleanup job
        // drains it batch by batch instead of materializing every row at once.
        const int batchSize = 1000;

        var results = await Context.UserSessions
            .Where(s => !s.IsRevoked && s.ExpiresAt < cutoffTime)
            .OrderBy(s => s.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }
}
