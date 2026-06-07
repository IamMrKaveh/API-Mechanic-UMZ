using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Security.Services;

public sealed class RateLimitService(IConnectionMultiplexer redis) : IRateLimitService
{
    private readonly IDatabase _db = redis.GetDatabase();

    private const string SlidingWindowScript = @"
local key = KEYS[1]
local now = tonumber(ARGV[1])
local window = tonumber(ARGV[2])
local max_requests = tonumber(ARGV[3])
local request_id = ARGV[4]

local window_start = now - window
redis.call('ZREMRANGEBYSCORE', key, 0, window_start)

local current = redis.call('ZCARD', key)
if current >= max_requests then
    local ttl = redis.call('PTTL', key)
    if ttl < 0 then ttl = window * 1000 end
    return {1, ttl}
end

redis.call('ZADD', key, now, request_id)
redis.call('PEXPIRE', key, window * 1000)
return {0, 0}
";

    public async Task<(bool IsLimited, TimeSpan? RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxRequests,
        int windowSeconds)
    {
        var redisKey = $"ratelimit:{key}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var requestId = $"{now}:{Guid.NewGuid():N}";

        var result = (RedisResult[])(await _db.ScriptEvaluateAsync(
            SlidingWindowScript,
            keys: new RedisKey[] { redisKey },
            values: new RedisValue[]
            {
                now,
                windowSeconds,
                maxRequests,
                requestId
            }))!;

        var isLimited = (long)result[0] == 1;
        var ttlMs = (long)result[1];

        if (!isLimited) return (false, null);

        var retryAfter = ttlMs > 0
            ? TimeSpan.FromMilliseconds(ttlMs)
            : TimeSpan.FromSeconds(windowSeconds);

        return (true, retryAfter);
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync($"ratelimit:{key}");
    }
}