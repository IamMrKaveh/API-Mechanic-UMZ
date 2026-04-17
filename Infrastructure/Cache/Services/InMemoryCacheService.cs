using Application.Cache.Contracts;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Infrastructure.Cache.Services;

public sealed class InMemoryCacheService(IMemoryCache cache) : ICacheService
{
    private readonly ConcurrentDictionary<string, bool> _trackedKeys = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(30)
        };
        cache.Set(key, value, options);
        _trackedKeys.TryAdd(key, true);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var keys = _trackedKeys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keys)
        {
            cache.Remove(key);
            _trackedKeys.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return Task.FromResult(cache.TryGetValue(key, out _));
    }

    public Task ClearAsync(string key, CancellationToken ct)
    {
        cache.Remove(key);
        _trackedKeys.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}