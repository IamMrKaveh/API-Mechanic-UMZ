using Application.Common.Interfaces.Cache;
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
            if (!_redis.IsConnected) return null;
            var value = await _database.StringGetAsync(key);
            if (value.HasValue) return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error getting cache for key {Key}", key); }
        return null;
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, IEnumerable<string>? tags = null) where T : class
    {
        try
        {
            if (!_redis.IsConnected) return false;
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
        catch (Exception ex) { _logger.LogError(ex, "Error setting cache for key {Key}", key); return false; }
    }

    public Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class => SetAsync(key, value, expiry, null);

    public async Task ClearByPrefixAsync(string prefix)
    {
        try
        {
            if (!_redis.IsConnected) return;
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keysToDelete = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*")) keysToDelete.Add(key);
            if (keysToDelete.Any()) await _database.KeyDeleteAsync(keysToDelete.ToArray());
        }
        catch (Exception ex) { _logger.LogError(ex, "Error clearing cache for prefix {Prefix}", prefix); }
    }

    public async Task ClearByTagAsync(string tag)
    {
        try
        {
            if (!_redis.IsConnected) return;
            var tagKey = $"tag:{tag}";
            var keysToDelete = await _database.SetMembersAsync(tagKey);
            if (keysToDelete.Length > 0) await _database.KeyDeleteAsync(keysToDelete.Select(k => (RedisKey)k.ToString()).ToArray());
            await _database.KeyDeleteAsync(tagKey);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error clearing cache for tag {Tag}", tag); }
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        try
        {
            if (!_redis.IsConnected) return true;
            var lockKey = $"lock:{key}";
            var lockValue = Guid.NewGuid().ToString();
            return await _database.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error acquiring lock {Key}", key); return true; }
    }

    public async Task<bool> AcquireLockWithRetryAsync(string key, TimeSpan expiry, int retryCount = 3, int retryDelayMs = 500)
    {
        for (int i = 0; i < retryCount; i++)
        {
            if (await AcquireLockAsync(key, expiry)) return true;
            await Task.Delay(retryDelayMs);
        }
        return false;
    }

    public async Task ReleaseLockAsync(string key)
    {
        try
        {
            if (!_redis.IsConnected) return;
            await _database.KeyDeleteAsync($"lock:{key}");
        }
        catch (Exception ex) { _logger.LogError(ex, "Error releasing lock {Key}", key); }
    }

    public async Task ClearAsync(string key)
    {
        try { if (_redis.IsConnected) await _database.KeyDeleteAsync(key); }
        catch (Exception ex) { _logger.LogError(ex, "Error clearing cache {Key}", key); }
    }
}