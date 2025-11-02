namespace MainApi.Services.Redis;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task ClearByPrefixAsync(string prefix);
    Task<bool> AcquireLockAsync(string key, TimeSpan expiry);
    Task ReleaseLockAsync(string key);
}

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
}

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger, IMemoryCache memoryCache)
    {
        _database = redis.GetDatabase();
        _logger = logger;
        _memoryCache = memoryCache;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (!_redis.IsConnected)
            {
                return _memoryCache.Get<T>(key);
            }

            var value = await _database.StringGetAsync(key);
            if (value.HasValue)
            {
                var result = JsonSerializer.Deserialize<T>(value!);
                _memoryCache.Set(key, result, TimeSpan.FromMinutes(1));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key {Key}", key);
            return _memoryCache.Get<T>(key);
        }
        return null;
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _memoryCache.Set(key, value, expiry ?? TimeSpan.FromMinutes(10));
                return true;
            }

            var stringValue = JsonSerializer.Serialize(value);
            var result = await _database.StringSetAsync(key, stringValue, expiry ?? TimeSpan.FromMinutes(10));

            if (result)
            {
                _memoryCache.Set(key, value, TimeSpan.FromMinutes(1));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {Key}", key);
            _memoryCache.Set(key, value, expiry ?? TimeSpan.FromMinutes(10));
            return false;
        }
    }

    public async Task ClearByPrefixAsync(string prefix)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                return;
            }

            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Any())
            {
                await _database.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for prefix {Prefix}", prefix);
        }
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                return _memoryCache.Get(key) == null;
            }

            var lockKey = $"lock:{key}";
            var lockValue = Guid.NewGuid().ToString();
            return await _database.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key {Key}", key);
            return _memoryCache.Get(key) == null;
        }
    }

    public async Task ReleaseLockAsync(string key)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _memoryCache.Remove(key);
                return;
            }

            var lockKey = $"lock:{key}";
            await _database.KeyDeleteAsync(lockKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key {Key}", key);
            _memoryCache.Remove(key);
        }
    }
}