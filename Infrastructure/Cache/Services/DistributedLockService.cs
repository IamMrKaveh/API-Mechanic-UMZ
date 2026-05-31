using Infrastructure.Cache.Redis.Lock;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Services;

public sealed class DistributedLockService(
    IConnectionMultiplexer redis,
    IAuditService auditService) : IDistributedLock
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<ILockHandle?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        var key = $"lock:{resource}";
        var token = Guid.NewGuid().ToString("N");

        var acquired = await _db.StringSetAsync(key, token, expiry, When.NotExists);
        if (!acquired) return null;

        return new RedisLockHandle(_db, key, token, auditService);
    }
}