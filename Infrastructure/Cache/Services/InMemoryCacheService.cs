namespace Infrastructure.Cache.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, object> _locks = new();
    private readonly ILogger<InMemoryCacheService> _logger;

    public InMemoryCacheService(IMemoryCache cache, ILogger<InMemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        else
        {
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // default
        }
        _cache.Set(key, value, options);
        return Task.FromResult(true);
    }

    public Task ClearAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearByPrefixAsync(string prefix)
    {
        // IMemoryCache does not support prefix-based removal natively.
        // For production, use a tracking mechanism or Redis.
        _logger.LogWarning("ClearByPrefixAsync is not fully supported with InMemoryCache. Prefix: {Prefix}", prefix);
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        var lockKey = $"lock:{key}";
        if (_locks.TryAdd(lockKey, new object()))
        {
            _cache.Set(lockKey + "_expiry", true, expiry);
            return Task.FromResult(true);
        }

        // Check if the lock expired
        if (!_cache.TryGetValue(lockKey + "_expiry", out _))
        {
            _locks.TryRemove(lockKey, out _);
            if (_locks.TryAdd(lockKey, new object()))
            {
                _cache.Set(lockKey + "_expiry", true, expiry);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task ReleaseLockAsync(string key)
    {
        var lockKey = $"lock:{key}";
        _locks.TryRemove(lockKey, out _);
        _cache.Remove(lockKey + "_expiry");
        return Task.CompletedTask;
    }

    public async Task<bool> AcquireLockWithRetryAsync(string key, TimeSpan expiry, int retryCount = 3, int retryDelayMs = 500)
    {
        for (int i = 0; i < retryCount; i++)
        {
            if (await AcquireLockAsync(key, expiry))
            {
                return true;
            }
            await Task.Delay(retryDelayMs);
        }
        return false;
    }
}