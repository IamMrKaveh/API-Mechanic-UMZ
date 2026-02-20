namespace Infrastructure.Security.Services;

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(IConnectionMultiplexer redis, ILogger<RateLimitService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<(bool IsLimited, int RetryAfterSeconds)> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Redis is not connected. Rate limiting is bypassed.");
            return (false, 0);
        }

        try
        {
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromMinutes(windowMinutes);
            var windowStart = (now - window).ToUnixTimeSeconds();

            var transaction = db.CreateTransaction();

            var keyExpiryTask = transaction.KeyTimeToLiveAsync(key);

            _ = transaction.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
            var countTask = transaction.SortedSetLengthAsync(key);
            _ = transaction.SortedSetAddAsync(key, now.ToUnixTimeSeconds().ToString(), now.ToUnixTimeSeconds());
            _ = transaction.KeyExpireAsync(key, window.Add(TimeSpan.FromSeconds(10)), ExpireWhen.Always); // Add a buffer

            if (await transaction.ExecuteAsync())
            {
                var count = await countTask;
                if (count >= maxAttempts)
                {
                    var ttl = await keyExpiryTask;
                    var retryAfter = ttl.HasValue ? (int)ttl.Value.TotalSeconds : (int)window.TotalSeconds;
                    return (true, retryAfter);
                }
                return (false, 0);
            }

            _logger.LogWarning("Redis transaction for rate limiting failed to execute for key: {Key}", key);
            return (false, 0);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error during rate limiting for key: {Key}", key);
            return (false, 0);
        }
    }
}