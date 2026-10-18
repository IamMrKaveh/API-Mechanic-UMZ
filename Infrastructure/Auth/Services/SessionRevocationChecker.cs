using Application.Auth.Contracts;
using Application.Cache.Contracts;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Infrastructure.Auth.Services;

/// <summary>
/// Checks session liveness with a short-lived cache so the JWT hot path costs
/// at most one lookup per session per TTL window. Revocations propagate within
/// the negative-cache window.
/// </summary>
public sealed class SessionRevocationChecker(
    ISessionRepository sessionRepository,
    ICacheService cacheService,
    IDateTimeProvider dateTimeProvider) : ISessionRevocationChecker
{
    private static readonly TimeSpan ActiveCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RevokedCacheTtl = TimeSpan.FromSeconds(10);

    public async Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken ct = default)
    {
        var cacheKey = $"auth:session:active:{sessionId:N}";

        var cached = await cacheService.GetAsync<bool?>(cacheKey, ct);
        if (cached.HasValue)
            return cached.Value;

        var session = await sessionRepository.GetByIdAsync(SessionId.From(sessionId), ct);
        var isActive = session?.IsActive(dateTimeProvider.UtcNow) ?? false;

        await cacheService.SetAsync(cacheKey, isActive, isActive ? ActiveCacheTtl : RevokedCacheTtl, ct);

        return isActive;
    }
}
