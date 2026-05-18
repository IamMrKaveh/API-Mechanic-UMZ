namespace Infrastructure.Cache.Redis.Lock;

public sealed class NoOpDistributedLock(ILogger<NoOpDistributedLock> logger) : IDistributedLock
{
    public async Task<ILockHandle?> TryAcquireAsync(string resource)
    {
        logger.LogDebug("Distributed locks are disabled. Acquiring no-op lock for resource: {Resource}", resource);
        return await Task.FromResult<ILockHandle?>(new NoOpLockHandle(resource, logger));
    }

    public async Task<ILockHandle?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        logger.LogDebug("Distributed locks are disabled. Acquiring no-op lock for resource: {Resource}", resource);
        return await Task.FromResult<ILockHandle>(new NoOpLockHandle(resource, logger));
    }
}

public sealed class NoOpLockHandle(string resource, ILogger logger) : ILockHandle
{
    private bool _released = false;

    public string Resource => resource;
    public bool IsAcquired => !_released;

    public ValueTask DisposeAsync() => new(ReleaseAsync());

    public async Task ReleaseAsync()
    {
        if (_released)
        {
            await Task.CompletedTask;
            return;
        }
        _released = true;
        logger.LogInformation("Releasing no-op lock for resource: {Resource}", resource);
        await Task.CompletedTask;
    }
}