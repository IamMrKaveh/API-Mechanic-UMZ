using System.Text.Json;
using Application.Cache.Contracts;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Cache.Services;

public sealed class CacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await cache.GetStringAsync(key, ct);
        if (string.IsNullOrEmpty(data)) return default;
        return JsonSerializer.Deserialize<T>(data, SerializerOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;
        else
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

        var data = JsonSerializer.Serialize(value, SerializerOptions);
        await cache.SetStringAsync(key, data, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        await cache.RemoveAsync(prefix, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var data = await cache.GetStringAsync(key, ct);
        return data is not null;
    }

    public async Task ClearAsync(string key, CancellationToken ct)
    {
        await cache.RemoveAsync(key, ct);
    }
}