namespace Infrastructure.Cache.Services;

/// <summary>
/// سرویس Cache No-Op که زمانی استفاده می‌شود که Redis/Cache غیرفعال است
/// </summary>
public class NoOpCacheService : ICacheService
{
    private readonly ILogger<NoOpCacheService> _logger;

    public NoOpCacheService(ILogger<NoOpCacheService> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _logger.LogDebug("Cache is disabled. Cache miss for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        _logger.LogDebug("Cache is disabled. Skipping set for key: {Key}", key);
        return Task.FromResult(false);
    }

    public Task ClearAsync(string key)
    {
        _logger.LogDebug("Cache is disabled. Skipping clear for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task ClearByPrefixAsync(string prefix)
    {
        _logger.LogDebug("Cache is disabled. Skipping clear by prefix: {Prefix}", prefix);
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        _logger.LogDebug("Cache locks are disabled. Acquiring no-op lock for key: {Key}", key);
        return Task.FromResult(true);
    }

    public Task ReleaseLockAsync(string key)
    {
        _logger.LogDebug("Cache locks are disabled. Releasing no-op lock for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockWithRetryAsync(
        string key,
        TimeSpan expiry,
        int retryCount = 3,
        int retryDelayMs = 500)
    {
        _logger.LogDebug("Cache locks are disabled. Acquiring no-op lock with retry for key: {Key}", key);
        return Task.FromResult(true);
    }
}