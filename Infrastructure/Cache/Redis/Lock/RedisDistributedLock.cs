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
public sealed class RedisDistributedLock(
    IConnectionMultiplexer redis,
    IAuditService auditService) : IDistributedLock
{
    private readonly IDatabase _db = redis.GetDatabase();

    private static readonly TimeSpan DefaultLockTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(200);
    private const int DefaultRetryCount = 5;

    /// <summary>
    /// تلاش برای گرفتن قفل.
    /// </summary>
    public async Task<ILockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var lockKey = $"lock:{resource}";
        var lockValue = Guid.NewGuid().ToString("N");

        for (var attempt = 0; expiry; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var acquired = await _db.StringSetAsync(
                lockKey, lockValue, expiry, When.NotExists);

            if (acquired)
            {
                await auditService.LogDebugAsync("[DistributedLock] Acquired lock '{Reresourcesource}' (attempt {attempt + 1}/{retryCount + 1})", ct);

                return new RedisLockHandle(_db, lockKey, lockValue, auditService);
            }

            if (attempt < retryCount)
            {
                var waitTime = TimeSpan.FromMilliseconds(
                    delay.TotalMilliseconds * Math.Pow(1.5, attempt));

                await auditService.LogDebugAsync("[DistributedLock] Lock '{resource}' busy. Retry {attempt + 1}/{retryCount} after {waitTime.TotalMilliseconds}ms", ct);

                await Task.Delay(waitTime, ct);
            }
        }

        await auditService.LogWarningAsync("[DistributedLock] Failed to acquire lock '{resource}' after {retryCount + 1} attempts.", ct);

        return null;
    }

    /// <summary>
    /// گرفتن قفل با اطمینان (در صورت عدم موفقیت استثنا می‌دهد).
    /// </summary>
    public async Task<ILockHandle?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var handle = await TryAcquireAsync(resource, expiry, ct);
        if (handle is null)
            throw new DistributedLockException(
                $"امکان گرفتن قفل '{resource}' وجود ندارد. لطفاً دوباره تلاش کنید.");

        return handle;
    }
}