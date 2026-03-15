using Domain.Security.Aggregates;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Security.Interfaces;

public interface ISessionRepository
{
    Task<UserSession?> GetByIdAsync(
        UserSessionId sessionId,
        CancellationToken ct = default);

    Task<UserSession?> GetByRefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserSession>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<int> GetActiveSessionCountAsync(
        UserId userId,
        CancellationToken ct = default);

    Task AddAsync(
        UserSession session,
        CancellationToken ct = default);

    void Update(UserSession session);

    Task RevokeAsync(
        UserSessionId sessionId,
        CancellationToken ct = default);

    Task RevokeAllByUserAsync(
        UserId userId,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserSession>> GetExpiredActiveSessionsAsync(
        DateTime cutoffTime,
        CancellationToken ct = default);
}