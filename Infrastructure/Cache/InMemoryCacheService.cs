using Application.Common.Interfaces.Cache;

namespace Infrastructure.Cache;

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

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, IEnumerable<string>? tags = null) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.SetAbsoluteExpiration(expiry.Value);
        }
        _cache.Set(key, value, options);
        return Task.FromResult(true);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        return SetAsync(key, value, expiry, null);
    }

    public Task ClearAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearByPrefixAsync(string prefix)
    {
        return Task.CompletedTask;
    }

    public Task ClearByTagAsync(string tag)
    {
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        if (_locks.TryAdd(key, new object()))
        {
            _cache.Set(key + "_lock_expiry", true, expiry);
            return Task.FromResult(true);
        }

        if (!_cache.TryGetValue(key + "_lock_expiry", out _))
        {
            _locks.TryRemove(key, out _);
            return Task.FromResult(_locks.TryAdd(key, new object()));
        }

        return Task.FromResult(false);
    }

    public Task ReleaseLockAsync(string key)
    {
        _locks.TryRemove(key, out _);
        _cache.Remove(key + "_lock_expiry");
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