using Application.Cache.Contracts;
using Infrastructure.Cache.Options;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Services;

public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<CacheOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly CacheOptions _options = options.Value;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private string PrefixKey(string key) => $"{_options.KeyPrefix}:{key}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(PrefixKey(key));
            if (!value.HasValue) return default;
            return JsonSerializer.Deserialize<T>(value!, SerializerOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache get failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, SerializerOptions);
            var exp = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            await _db.StringSetAsync(PrefixKey(key), serialized, exp);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache set failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(PrefixKey(key));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache remove failed for key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints().First());
            var keys = server.Keys(pattern: $"{_options.KeyPrefix}:{prefix}*").ToArray();
            if (keys.Length > 0)
                await _db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache remove by prefix failed for prefix {Prefix}", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            return await _db.KeyExistsAsync(PrefixKey(key));
        }
        catch
        {
            return false;
        }
    }

    public async Task ClearAsync(string key, CancellationToken ct)
    {
        await RemoveAsync(key, ct);
    }
}