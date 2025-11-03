namespace MainApi.Services.Redis;

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

    public async Task ClearAsync(string key)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _memoryCache.Remove(key);
                return;
            }

            await _database.KeyDeleteAsync(key);
            _memoryCache.Remove(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for key {Key}", key);
            _memoryCache.Remove(key);
        }
    }

}