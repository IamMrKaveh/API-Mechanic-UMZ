namespace MainApi.Services.Redis;

public class MockRedisDatabase : ICacheService
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime Expiry)> _memory = new();
    private readonly ConcurrentDictionary<string, (string LockId, DateTime Expiry)> _locks = new();
    private readonly Timer _cleanupTimer;

    public MockRedisDatabase()
    {
        _cleanupTimer = new Timer(CleanupExpiredKeys, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        if (_memory.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > DateTime.UtcNow)
            {
                var result = JsonSerializer.Deserialize<T>(entry.Value);
                return Task.FromResult(result);
            }
            _memory.TryRemove(key, out _);
        }
        return Task.FromResult<T?>(null);
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var expiryTime = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : DateTime.UtcNow.AddMinutes(10);
        var stringValue = JsonSerializer.Serialize(value);
        _memory[key] = (stringValue, expiryTime);
        return Task.FromResult(true);
    }

    public Task ClearByPrefixAsync(string prefix)
    {
        var keysToRemove = _memory.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keysToRemove)
            _memory.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        var lockKey = $"lock:{key}";
        var now = DateTime.UtcNow;

        if (_locks.TryGetValue(lockKey, out var existingLock))
        {
            if (existingLock.Expiry > now)
            {
                return Task.FromResult(false);
            }
            _locks.TryRemove(lockKey, out _);
        }

        var lockId = Guid.NewGuid().ToString();
        _locks[lockKey] = (lockId, now.Add(expiry));
        return Task.FromResult(true);
    }

    public Task ReleaseLockAsync(string key)
    {
        var lockKey = $"lock:{key}";
        _locks.TryRemove(lockKey, out _);
        return Task.CompletedTask;
    }

    private void CleanupExpiredKeys(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _memory.Where(kvp => kvp.Value.Expiry <= now).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
            _memory.TryRemove(key, out _);

        var expiredLocks = _locks.Where(kvp => kvp.Value.Expiry <= now).Select(kvp => kvp.Key).ToList();
        foreach (var lockKey in expiredLocks)
            _locks.TryRemove(lockKey, out _);
    }

    public Task ClearAsync(string key)
    {
        _memory.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
