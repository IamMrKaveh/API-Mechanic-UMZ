using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Auth.Repositories;

public class SessionRepository(DBContext context) : ISessionRepository
{
    private readonly DBContext _context = context;

    public async Task<UserSession?> GetBySelectorAsync(
        string tokenSelector,
        CancellationToken ct = default)
    {
        return await _context.UserSessions
            .FirstOrDefaultAsync(s => s.TokenSelector == tokenSelector, ct);
    }

    public async Task AddAsync(
        UserSession session,
        CancellationToken ct = default)
    {
        await _context.UserSessions.AddAsync(session, ct);
    }

    public async Task RevokeAsync(
        SessionId sessionId,
        CancellationToken ct = default)
    {
        var session = await _context.UserSessions.FindAsync([sessionId], ct);
        session?.Revoke();
    }

    public async Task RevokeAllByUserAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke();
    }

    public Task<UserSession?> GetByIdAsync(SessionId sessionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
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

    public void Update(UserSession session)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(DateTime cutoffTime, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}