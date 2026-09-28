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
    public async Task<UserSession?> GetByRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        return await Context.UserSessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, ct);
    }

    public Task RevokeAllByUserIdAsync(UserId userId, CancellationToken ct = default)
        => RevokeAllByUserIdAsync(userId, SessionRevocationReason.AllSessionsRevoked, ct);

    public async Task RevokeAllByUserIdAsync(UserId userId, SessionRevocationReason reason, CancellationToken ct = default)
    {
        var sessions = await Context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(ct);

        var now = dateTimeProvider.UtcNow;
        foreach (var session in sessions)
            session.Revoke(now, reason);
    }

    public async Task RevokeAllExceptAsync(UserId userId, SessionId exceptSessionId, SessionRevocationReason reason, CancellationToken ct = default)
    {
        var sessions = await Context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.Id != exceptSessionId)
            .ToListAsync(ct);

        var now = dateTimeProvider.UtcNow;
        foreach (var session in sessions)
            session.Revoke(now, reason);
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
        var results = await Context.UserSessions
            .Where(s => !s.IsRevoked && s.ExpiresAt < cutoffTime)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }
}
