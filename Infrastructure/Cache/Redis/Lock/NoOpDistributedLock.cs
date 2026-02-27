namespace Infrastructure.Cache.Redis.Lock;

/// <summary>
/// قفل توزیع‌شده No-Op که زمانی استفاده می‌شود که Redis غیرفعال است
/// </summary>
public sealed class NoOpDistributedLock : IDistributedLock
{
    private readonly ILogger<NoOpDistributedLock> _logger;

    public NoOpDistributedLock(ILogger<NoOpDistributedLock> logger)
    {
        _logger = logger;
    }

    public Task<ILockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int retryCount = 5,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Distributed locks are disabled. Acquiring no-op lock for resource: {Resource}", resource);
        return Task.FromResult<ILockHandle?>(new NoOpLockHandle(resource, _logger));
    }

    public Task<ILockHandle> AcquireAsync(
        string resource,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int retryCount = 5,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Distributed locks are disabled. Acquiring no-op lock for resource: {Resource}", resource);
        return Task.FromResult<ILockHandle>(new NoOpLockHandle(resource, _logger));
    }
}

public sealed class NoOpLockHandle : ILockHandle
{
    private readonly string _resource;
    private readonly ILogger _logger;
    private bool _released;

    public NoOpLockHandle(string resource, ILogger logger)
    {
        _resource = resource;
        _logger = logger;
        _released = false;
    }

    public string Resource => _resource;
    public bool IsAcquired => !_released;

    public ValueTask DisposeAsync()
    {
        return new ValueTask(ReleaseAsync());
    }

    public Task ReleaseAsync()
    {
        if (_released) return Task.CompletedTask;
        _released = true;
        _logger.LogDebug("Releasing no-op lock for resource: {Resource}", _resource);
        return Task.CompletedTask;
    }
}