namespace Infrastructure.Cache.Services;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, object> _locks = new();
    private readonly ConcurrentDictionary<string, bool> _allKeys = new();
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
            options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
        }

        _cache.Set(key, value, options);
        _allKeys.TryAdd(key, true);

        return Task.FromResult(true);
    }

    public Task ClearAsync(string key)
    {
        _cache.Remove(key);
        _allKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearByPrefixAsync(string prefix)
    {
        var keysToRemove = _allKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _allKeys.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        var lockKey = $"lock:{key}";
        if (_locks.TryAdd(lockKey, new object()))
        {
            _cache.Set(lockKey + "_expiry", true, expiry);
            _allKeys.TryAdd(lockKey + "_expiry", true);
            return Task.FromResult(true);
        }

        if (!_cache.TryGetValue(lockKey + "_expiry", out _))
        {
            _locks.TryRemove(lockKey, out _);
            if (_locks.TryAdd(lockKey, new object()))
            {
                _cache.Set(lockKey + "_expiry", true, expiry);
                _allKeys.TryAdd(lockKey + "_expiry", true);
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
        _allKeys.TryRemove(lockKey + "_expiry", out _);
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