using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Services;

public sealed class DistributedLockService(IConnectionMultiplexer redis) : IDistributedLock
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

        return new RedisLockHandle(_db, key, token);
    }
}

public sealed class RedisLockHandle(
    IDatabase db,
    string key,
    string token) : ILockHandle
{
    public bool IsAcquired => true;

    public async Task ReleaseAsync()
    {
        var current = await db.StringGetAsync(key);
        if (current == token)
            await db.KeyDeleteAsync(key);
    }

    public async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
    }
}