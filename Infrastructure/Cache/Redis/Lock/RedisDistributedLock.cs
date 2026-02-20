using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Lock;

/// <summary>
/// Redis Distributed Lock - قفل توزیع‌شده برای جلوگیری از race condition.
///
/// ویژگی‌ها:
/// - Fencing Token (جلوگیری از اجرای مجدد پس از expiry)
/// - Automatic Retry با Exponential Backoff
/// - Auto-release از طریق TTL
/// - IAsyncDisposable برای استفاده با using statement
/// </summary>
public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisDistributedLock> _logger;

    private static readonly TimeSpan DefaultLockTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(200);
    private const int DefaultRetryCount = 5;

    public RedisDistributedLock(
        IConnectionMultiplexer redis,
        ILogger<RedisDistributedLock> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// تلاش برای گرفتن قفل.
    /// </summary>
    public async Task<ILockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int retryCount = DefaultRetryCount,
        CancellationToken ct = default)
    {
        var lockKey = $"lock:{resource}";
        var lockValue = Guid.NewGuid().ToString("N"); // Fencing Token
        var expiry = ttl ?? DefaultLockTtl;
        var delay = retryDelay ?? DefaultRetryDelay;

        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var acquired = await _db.StringSetAsync(
                lockKey, lockValue, expiry, When.NotExists);

            if (acquired)
            {
                _logger.LogDebug(
                    "[DistributedLock] Acquired lock '{Resource}' (attempt {Attempt}/{Max})",
                    resource, attempt + 1, retryCount + 1);

                return new RedisLockHandle(_db, lockKey, lockValue, _logger);
            }

            if (attempt < retryCount)
            {
                // Exponential Backoff
                var waitTime = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * Math.Pow(1.5, attempt));

                _logger.LogDebug(
                    "[DistributedLock] Lock '{Resource}' busy. Retry {Attempt}/{Max} after {Wait}ms",
                    resource, attempt + 1, retryCount, waitTime.TotalMilliseconds);

                await Task.Delay(waitTime, ct);
            }
        }

        _logger.LogWarning(
            "[DistributedLock] Failed to acquire lock '{Resource}' after {Retries} attempts.",
            resource, retryCount + 1);

        return null;
    }

    /// <summary>
    /// گرفتن قفل با اطمینان (در صورت عدم موفقیت استثنا می‌دهد).
    /// </summary>
    public async Task<ILockHandle> AcquireAsync(
        string resource,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int retryCount = DefaultRetryCount,
        CancellationToken ct = default)
    {
        var handle = await TryAcquireAsync(resource, ttl, retryDelay, retryCount, ct);
        if (handle is null)
            throw new DistributedLockException(
                $"امکان گرفتن قفل '{resource}' وجود ندارد. لطفاً دوباره تلاش کنید.");

        return handle;
    }
}