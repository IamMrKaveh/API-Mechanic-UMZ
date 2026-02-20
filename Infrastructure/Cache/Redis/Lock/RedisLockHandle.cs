using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Lock;

public sealed class RedisLockHandle : ILockHandle
{
    private readonly IDatabase _db;
    private readonly string _key;
    private readonly string _value;    // Fencing Token
    private readonly ILogger _logger;
    private bool _released;

    private static readonly string LuaRelease = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    public RedisLockHandle(IDatabase db, string key, string value, ILogger logger)
    {
        _db = db;
        _key = key;
        _value = value;
        _logger = logger;
    }

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
            // از Lua Script برای atomic check+delete استفاده می‌کنیم
            // این تضمین می‌کند فقط owner می‌تواند قفل را آزاد کند
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