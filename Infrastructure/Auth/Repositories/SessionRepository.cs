using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Auth.Repositories;

public sealed class SessionRepository(DBContext context) : ISessionRepository
{
    public async Task<UserSession?> GetByIdAsync(SessionId id, CancellationToken ct = default)
    {
        return await context.UserSessions.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<UserSession?> GetBySelectorAsync(string selector, CancellationToken ct = default)
    {
        return await context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Selector == selector, ct);
    }

    public async Task<IReadOnlyList<UserSession>> GetActiveSessionsByUserIdAsync(
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

    public async Task AddAsync(UserSession session, CancellationToken ct = default)
    {
        await context.UserSessions.AddAsync(session, ct);
    }

    public void Update(UserSession session)
    {
        context.UserSessions.Update(session);
    }

    public async Task RevokeAllByUserIdAsync(
        UserId userId,
        SessionRevocationReason reason,
        CancellationToken ct = default)
    {
        var sessions = await context.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke(reason);
    }

    public async Task DeleteExpiredAsync(DateTime before, CancellationToken ct = default)
    {
        var expired = await context.UserSessions
            .Where(s => s.ExpiresAt < before)
            .ToListAsync(ct);

        context.UserSessions.RemoveRange(expired);
    }

    public Task<UserSession?> GetByRefreshTokenAsync(RefreshToken refreshToken, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetActiveSessionCountAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeAsync(SessionId sessionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeAllByUserAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(DateTime cutoffTime, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}