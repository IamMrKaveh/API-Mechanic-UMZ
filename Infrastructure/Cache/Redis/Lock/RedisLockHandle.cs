using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Cache.Redis.Lock;

public sealed class RedisLockHandle(
    IDatabase db,
    string key,
    string value,
    IAuditService auditService) : ILockHandle
{
    private bool _released;

    private static readonly string LuaRelease = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

    public string Resource => key;
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
            await db.ScriptEvaluateAsync(
                LuaRelease,
                [key],
                [value]);

            await auditService.LogDebugAsync($"Released lock '{key}'");
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync($"Failed to release lock '{key}'");
        }
    }
}