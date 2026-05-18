namespace Infrastructure.Security.Services;

public sealed class InMemoryRateLimitService(IMemoryCache cache) : IRateLimitService
{
    public Task<(bool IsLimited, TimeSpan? RetryAfterSeconds)> IsLimitedAsync(
        string key,
        int maxAttempts = 5,
        int windowMinutes = 15)
    {
        var windowKey = $"ratelimit:{key}";
        var window = TimeSpan.FromMinutes(windowMinutes);
        var now = DateTimeOffset.UtcNow;

        var timestamps = cache.GetOrCreate(windowKey, entry =>
        {
            entry.SlidingExpiration = window;
            return new List<DateTimeOffset>();
        })!;

        lock (timestamps)
        {
            var cutoff = now - window;
            timestamps.RemoveAll(t => t < cutoff);

            if (timestamps.Count >= maxAttempts)
            {
                var retryAfter = timestamps.Min() + window - now;
                return Task.FromResult<(bool, TimeSpan?)>((true, retryAfter));
            }

            timestamps.Add(now);
        }

        return Task.FromResult<(bool, TimeSpan?)>((false, null));
    }
}