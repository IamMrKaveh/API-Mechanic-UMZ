using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Security.Services;

public sealed class RateLimitService(IConnectionMultiplexer redis) : IRateLimitService
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<(bool IsLimited, TimeSpan? RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxRequests,
        int windowSeconds)
    {
        var redisKey = $"ratelimit:{key}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - windowSeconds;

        var transaction = _db.CreateTransaction();
        _ = transaction.SortedSetRemoveRangeByScoreAsync(redisKey, 0, windowStart);
        var countTask = transaction.SortedSetLengthAsync(redisKey);
        _ = transaction.SortedSetAddAsync(redisKey, now.ToString(), now);
        _ = transaction.KeyExpireAsync(redisKey, TimeSpan.FromSeconds(windowSeconds));
        await transaction.ExecuteAsync();

        var count = await countTask;

        if (count >= maxRequests)
        {
            var ttl = await _db.KeyTimeToLiveAsync(redisKey);
            return (true, ttl ?? TimeSpan.FromSeconds(windowSeconds));
        }

        return (false, null);
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync($"ratelimit:{key}");
    }
}