using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected. Cache Get operation for key {Key} is skipped.", key);
                return null;
            }

            var value = await _database.StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonSerializer.Deserialize<T>(value!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache for key {Key}", key);
        }
        return null;
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, IEnumerable<string>? tags = null) where T : class
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected. Cache Set operation for key {Key} is skipped.", key);
                return false;
            }

            var stringValue = JsonSerializer.Serialize(value);
            var expiryTime = expiry ?? TimeSpan.FromMinutes(10);

            var transaction = _database.CreateTransaction();
            _ = transaction.StringSetAsync(key, stringValue, expiryTime);

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    var tagKey = $"tag:{tag}";
                    _ = transaction.SetAddAsync(tagKey, key);
                    _ = transaction.KeyExpireAsync(tagKey, expiryTime.Add(TimeSpan.FromHours(1)));
                }
            }
            return await transaction.ExecuteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache for key {Key}", key);
            return false;
        }
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        return SetAsync(key, value, expiry, null);
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

            var keysToDelete = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                keysToDelete.Add(key);
            }

            if (keysToDelete.Any())
            {
                await _database.KeyDeleteAsync(keysToDelete.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for prefix {Prefix}", prefix);
        }
    }
    public async Task ClearByTagAsync(string tag)
    {
        try
        {
            if (!_redis.IsConnected) return;
            var tagKey = $"tag:{tag}";
            var keysToDelete = await _database.SetMembersAsync(tagKey);
            if (keysToDelete.Length > 0)
            {
                var redisKeys = keysToDelete.Select(k => (RedisKey)k.ToString()).ToArray();
                await _database.KeyDeleteAsync(redisKeys);
            }
            await _database.KeyDeleteAsync(tagKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for tag {Tag}", tag);
        }
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected. Bypassing lock for key {Key}.", key);
                return true;
            }

            var lockKey = $"lock:{key}";
            var lockValue = Guid.NewGuid().ToString();
            return await _database.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock for key {Key}. Proceeding without lock.", key);
            return true;
        }
    }

    public async Task ReleaseLockAsync(string key)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected. Lock for key {Key} might not be released.", key);
                return;
            }

            var lockKey = $"lock:{key}";
            await _database.KeyDeleteAsync(lockKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing lock for key {Key}", key);
        }
    }

    public async Task ClearAsync(string key)
    {
        try
        {
            if (!_redis.IsConnected)
            {
                _logger.LogWarning("Redis is not connected. Cache Clear for key {Key} is skipped.", key);
                return;
            }

            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for key {Key}", key);
        }
    }
}