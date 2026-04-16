namespace Infrastructure.Cache.Services;

/// <summary>
/// سرویس Cache No-Op که زمانی استفاده می‌شود که Redis/Cache غیرفعال است
/// </summary>
public class NoOpCacheService(IAuditService auditService) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        await auditService.LogDebugAsync($"Cache is disabled. Cache miss for key: {key}");
        return await Task.FromResult<T?>(null);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        await auditService.LogDebugAsync($"Cache is disabled. Skipping set for key: {key}");
        return await Task.FromResult(false);
    }

    public async Task ClearAsync(string key)
    {
        await auditService.LogDebugAsync("Cache is disabled. Skipping clear for key: {Key}");
        await Task.CompletedTask;
    }

    public async Task ClearByPrefixAsync(string prefix)
    {
        await auditService.LogDebugAsync("Cache is disabled. Skipping clear by prefix: {Prefix}");
        await Task.CompletedTask;
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        await auditService.LogDebugAsync("Cache locks are disabled. Acquiring no-op lock for key: {Key}");
        return await Task.FromResult(true);
    }

    public async Task ReleaseLockAsync(string key)
    {
        await auditService.LogDebugAsync("Cache locks are disabled. Releasing no-op lock for key: {Key}");
        await Task.CompletedTask;
    }

    public async Task<bool> AcquireLockWithRetryAsync(
        string key,
        TimeSpan expiry,
        int retryCount = 3,
        int retryDelayMs = 500)
    {
        await auditService.LogDebugAsync("Cache locks are disabled. Acquiring no-op lock with retry for key: {Key}");
        return await Task.FromResult(true);
    }
}