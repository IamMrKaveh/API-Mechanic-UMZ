using Application.Cache.Contracts;
using Application.Audit.Contracts;

namespace Infrastructure.Cache.Services;

public sealed class NoOpCacheService(IAuditService auditService) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        await auditService.LogDebugAsync($"[NoOpCache] GetAsync skipped for key: {key}", ct);
        return default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        await auditService.LogDebugAsync($"[NoOpCache] SetAsync skipped for key: {key}", ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await auditService.LogDebugAsync($"[NoOpCache] RemoveAsync skipped for key: {key}", ct);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        await auditService.LogDebugAsync($"[NoOpCache] RemoveByPrefixAsync skipped for prefix: {prefix}", ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        await auditService.LogDebugAsync($"[NoOpCache] ExistsAsync skipped for key: {key}", ct);
        return false;
    }

    public async Task ClearAsync(string key, CancellationToken ct)
    {
        await auditService.LogDebugAsync($"[NoOpCache] ClearAsync skipped for key: {key}", ct);
    }
}