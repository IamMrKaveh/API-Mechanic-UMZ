namespace Infrastructure.Cache.Redis.Services;

public class RedisCacheService : ICacheService
{
    private readonly StackExchange.Redis.IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _redis;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Cache miss for key: {Key}", key);
            return default;
        }

        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>(value.ToString(), _jsonOptions);
            }
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return default;
        }
    }

    public async Task ClearAsync(string key)
    {
        if (!_redis.IsConnected) return;

        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache key: {Key}", key);
        }
    }

    public async Task ClearByPrefixAsync(string prefix)
    {
        if (!_redis.IsConnected) return;

        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: prefix + "*").ToArray();
            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache by prefix: {Prefix}", prefix);
        }
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        if (!_redis.IsConnected) return false;

        try
        {
            return await _database.StringSetAsync($"lock:{key}", "1", expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock: {Key}", key);
            return false;
        }
    }

    public async Task ReleaseLockAsync(string key)
    {
        if (!_redis.IsConnected) return;

        try
        {
            await _database.KeyDeleteAsync($"lock:{key}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock: {Key}", key);
        }
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