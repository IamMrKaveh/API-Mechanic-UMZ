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

    public async Task AddAsync(UserSession session, CancellationToken ct = default)
    {
        await context.UserSessions.AddAsync(session, ct);
    }

    public void Update(UserSession session)
    {
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