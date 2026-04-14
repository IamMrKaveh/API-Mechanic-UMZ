using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Auth.Repositories;

public sealed class SessionRepository(DBContext context) : ISessionRepository
{
    public async Task<UserSession?> GetByIdAsync(SessionId sessionId, CancellationToken ct = default)
    {
        return await context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public async Task<UserSession?> GetByRefreshTokenAsync(
        RefreshToken refreshToken,
        CancellationToken ct = default)
    {
        return await context.UserSessions
            .FirstOrDefaultAsync(s => s.RefreshToken.Value == refreshToken.Value, ct);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var results = await context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<int> GetActiveSessionCountAsync(UserId userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await context.UserSessions
            .CountAsync(s => s.UserId == userId && !s.IsRevoked && s.ExpiresAt > now, ct);
    }

    public async Task AddAsync(UserSession session, CancellationToken ct = default)
    {
        await context.UserSessions.AddAsync(session, ct);
    }

    public void Update(UserSession session)
    {
        context.UserSessions.Update(session);
    }

    public async Task RevokeAsync(SessionId sessionId, CancellationToken ct = default)
    {
        var session = await context.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
        if (session is null || !session.IsActive) return;

        session.Revoke(SessionRevocationReason.UserRequested);
        context.UserSessions.Update(session);
    }

    public async Task RevokeAllByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        var sessions = await context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke(SessionRevocationReason.AllSessionsRevoked);
    }

    public async Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default)
    {
        var results = await context.UserSessions
            .Where(s => !s.IsRevoked && s.ExpiresAt < cutoffTime)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }
}