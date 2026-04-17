using Application.Audit.Contracts;
using Application.Cache.Contracts;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Lock;

public sealed class RedisDistributedLock(
    IConnectionMultiplexer redis,
    IAuditService auditService) : IDistributedLock
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const int DefaultRetryCount = 5;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task<ILockHandle?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var lockKey = $"lock:{resource}";
        var lockValue = Guid.NewGuid().ToString("N");

        for (var attempt = 0; attempt <= DefaultRetryCount; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var acquired = await _db.StringSetAsync(lockKey, lockValue, expiry, When.NotExists);

            if (acquired)
            {
                await auditService.LogDebugAsync(
                    $"[DistributedLock] Acquired lock '{resource}' on attempt {attempt + 1}", ct);
                return new RedisLockHandle(_db, lockKey, lockValue, auditService);
            }

            if (attempt < DefaultRetryCount)
            {
                var waitTime = TimeSpan.FromMilliseconds(
                    DefaultRetryDelay.TotalMilliseconds * Math.Pow(1.5, attempt));
                await Task.Delay(waitTime, ct);
            }
        }

        await auditService.LogWarningAsync(
            $"[DistributedLock] Failed to acquire lock '{resource}' after {DefaultRetryCount + 1} attempts.", ct);
        return null;
    }
}