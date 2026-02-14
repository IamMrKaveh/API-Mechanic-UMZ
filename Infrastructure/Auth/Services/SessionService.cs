namespace Infrastructure.Auth.Services;

/// <summary>
/// پیاده‌سازی مدیریت نشست‌ها با Database
/// در صورت نیاز می‌توان لایه Redis Cache اضافه کرد
/// </summary>
public class SessionService : ISessionService
{
    private readonly LedkaContext _context;
    private readonly ILogger<SessionService> _logger;

    public SessionService(LedkaContext context, ILogger<SessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserSessionInfo> CreateSessionAsync(
        int userId,
        string tokenSelector,
        string tokenVerifierHash,
        string ipAddress,
        string? userAgent,
        string sessionType = "Web",
        int expiryDays = 30,
        CancellationToken ct = default)
    {
        // محدود کردن تعداد سشن‌های فعال (حداکثر ۵)
        await EnforceMaxActiveSessionsAsync(userId, 5, ct);

        var session = UserSession.Create(
            userId,
            tokenSelector,
            tokenVerifierHash,
            ipAddress,
            userAgent,
            sessionType,
            expiryDays
        );

        await _context.UserSessions.AddAsync(session, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Session created for user {UserId} from IP {IpAddress}.",
            userId, ipAddress);

        return MapToSessionInfo(session);
    }

    public async Task<UserSessionInfo?> GetSessionBySelectorAsync(
        string tokenSelector, CancellationToken ct = default)
    {
        var session = await _context.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenSelector == tokenSelector, ct);

        return session == null ? null : MapToSessionInfo(session);
    }

    public async Task<bool> ValidateSessionAsync(
        string tokenSelector, string tokenVerifierHash, CancellationToken ct = default)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s =>
                s.TokenSelector == tokenSelector &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTime.UtcNow, ct);

        if (session == null)
            return false;

        if (session.TokenVerifierHash != tokenVerifierHash)
            return false;

        // ثبت فعالیت
        session.RecordActivity();
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task RevokeSessionAsync(int sessionId, CancellationToken ct = default)
    {
        var session = await _context.UserSessions.FindAsync(new object[] { sessionId }, ct);
        if (session != null && session.RevokedAt == null)
        {
            session.Revoke();
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Session {SessionId} revoked.", sessionId);
        }
    }

    public async Task RevokeAllUserSessionsAsync(int userId, CancellationToken ct = default)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var session in sessions)
        {
            session.Revoke();
        }

        if (sessions.Any())
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation(
                "All {Count} active sessions revoked for user {UserId}.",
                sessions.Count, userId);
        }
    }

    public async Task<IEnumerable<UserSessionInfo>> GetActiveSessionsAsync(
        int userId, CancellationToken ct = default)
    {
        var sessions = await _context.UserSessions
            .AsNoTracking()
            .Where(s =>
                s.UserId == userId &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync(ct);

        return sessions.Select(MapToSessionInfo);
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var expiredSessions = await _context.UserSessions
            .Where(s => s.ExpiresAt < cutoff)
            .ToListAsync(ct);

        if (expiredSessions.Any())
        {
            _context.UserSessions.RemoveRange(expiredSessions);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Cleaned up {Count} expired sessions.",
                expiredSessions.Count);
        }
    }

    private async Task EnforceMaxActiveSessionsAsync(
        int userId, int maxSessions, CancellationToken ct)
    {
        var activeSessions = await _context.UserSessions
            .Where(s =>
                s.UserId == userId &&
                s.RevokedAt == null &&
                s.ExpiresAt > DateTime.UtcNow)
            .OrderBy(s => s.LastActivityAt ?? s.CreatedAt)
            .ToListAsync(ct);

        while (activeSessions.Count >= maxSessions)
        {
            var oldest = activeSessions.First();
            oldest.Revoke();
            activeSessions.Remove(oldest);
        }

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync(ct);
        }
    }

    private static UserSessionInfo MapToSessionInfo(UserSession session)
    {
        return new UserSessionInfo
        {
            Id = session.Id,
            UserId = session.UserId,
            TokenSelector = session.TokenSelector,
            TokenVerifierHash = session.TokenVerifierHash,
            ExpiresAt = session.ExpiresAt,
            IsRevoked = session.RevokedAt.HasValue,
            RevokedAt = session.RevokedAt,
            CreatedByIp = session.CreatedByIp,
            UserAgent = session.UserAgent,
            ReplacedByTokenHash = session.ReplacedByTokenHash,
            SessionType = session.SessionType,
            LastActivityAt = session.LastActivityAt,
            CreatedAt = session.CreatedAt
        };
    }
}