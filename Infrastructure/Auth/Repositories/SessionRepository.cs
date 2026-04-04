using Domain.Security.Aggregates;
using Domain.Security.Interfaces;
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
        int sessionId,
        CancellationToken ct = default)
    {
        var session = await _context.UserSessions.FindAsync(new object[] { sessionId }, ct);
        session?.Revoke();
    }

    public async Task RevokeAllByUserAsync(
        int userId,
        CancellationToken ct = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
            session.Revoke();
    }
}