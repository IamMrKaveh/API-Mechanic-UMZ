using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);

            if (!value.HasValue)
                return null;

            return JsonSerializer.Deserialize<T>((string)value!);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}", key);
            return null;
        }
    }

    /// <summary>
    /// ذخیره مقدار در Cache با TTL
    /// </summary>
    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            return await _db.StringSetAsync(key, serialized, expiry ?? DefaultTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync(string key)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DELETE failed for key {Key}", key);
        }
    }

    public async Task ClearByPrefixAsync(string prefix)
    {
        try
        {
            var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Length > 0)
                await _db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DELETE by prefix failed for {Prefix}", prefix);
        }
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, "1", expiry, When.NotExists);
    }

    public async Task ReleaseLockAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> AcquireLockWithRetryAsync(
        string key, TimeSpan expiry, int retryCount = 3, int retryDelayMs = 500)
    {
        for (var i = 0; i < retryCount; i++)
        {
            if (await AcquireLockAsync(key, expiry)) return true;
            await Task.Delay(retryDelayMs);
        }
        return false;
    }
}