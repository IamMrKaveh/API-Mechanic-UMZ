namespace MainApi.Services.RateLimit;

public interface IRateLimitService
{
    Task<bool> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15);
}

public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;

    public RateLimitService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> IsLimitedAsync(string key, int maxAttempts = 5, int windowMinutes = 15)
    {
        var db = _redis.GetDatabase();
        var now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var window = System.TimeSpan.FromMinutes(windowMinutes);
        var windowStart = now - (long)window.TotalSeconds;

        var transaction = db.CreateTransaction();
        transaction.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart);
        var countTask = transaction.SortedSetLengthAsync(key);
        transaction.SortedSetAddAsync(key, now.ToString(), now);
        transaction.KeyExpireAsync(key, window, ExpireWhen.Always);

        if (await transaction.ExecuteAsync())
        {
            var count = await countTask;
            return count >= maxAttempts;
        }

        return true;
    }
}