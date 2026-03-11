using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Lock;

public sealed class RedisLockHandle(IDatabase db, string key, string value, ILogger logger) : ILockHandle
{
    private readonly IDatabase _db = db;
    private readonly string _key = key;
    private readonly string _value = value;
    private readonly ILogger _logger = logger;
    private bool _released;

    private static readonly string LuaRelease = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    public string Resource => _key;
    public bool IsAcquired => !_released;

    public async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
    }

    public async Task ReleaseAsync()
    {
        if (_released) return;
        _released = true;

        try
        {
            await _db.ScriptEvaluateAsync(
                LuaRelease,
                new RedisKey[] { _key },
                new RedisValue[] { _value });

            _logger.LogDebug("[DistributedLock] Released lock '{Key}'", _key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DistributedLock] Failed to release lock '{Key}'", _key);
        }
    }
}