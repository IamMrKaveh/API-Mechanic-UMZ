using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.RateLimit;

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(IConnectionMultiplexer redis, ILogger<RateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<bool> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Rate limiting is bypassed.");
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var window = TimeSpan.FromMinutes(windowMinutes);
            var windowStart = now - (long)window.TotalSeconds;

            var transaction = db.CreateTransaction();
            _ = transaction.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var countTask = transaction.SortedSetLengthAsync(key);
            _ = transaction.SortedSetAddAsync(key, now.ToString(), now);
            _ = transaction.KeyExpireAsync(key, window, ExpireWhen.Always);

            if (await transaction.ExecuteAsync())
            {
                var count = await countTask;
                return count >= maxAttempts;
            }

            _logger.LogWarning("Redis transaction for rate limiting failed to execute for key: {Key}", key);
            return false;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error during rate limiting for key: {Key}", key);
            return false;
        }
    }
}