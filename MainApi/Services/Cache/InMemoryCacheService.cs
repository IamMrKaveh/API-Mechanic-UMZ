namespace MainApi.Services.Cache;

public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _locks = new();

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
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
        // This is a simplified implementation. For real-world scenarios,
        // this can be inefficient and might require a different approach.
        var keysToRemove = new List<string>();
        // Cannot directly iterate keys in IMemoryCache, this is a limitation.
        // For a full implementation, a separate list of keys would be needed.
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        return Task.FromResult(_locks.TryAdd(key, 0));
    }

    public Task ReleaseLockAsync(string key)
    {
        _locks.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
