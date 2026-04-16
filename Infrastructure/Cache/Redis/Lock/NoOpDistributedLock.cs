namespace Infrastructure.Cache.Redis.Lock;

/// <summary>
/// قفل توزیع‌شده No-Op که زمانی استفاده می‌شود که Redis غیرفعال است
/// </summary>
public sealed class NoOpDistributedLock(IAuditService auditService) : IDistributedLock
{
    public async Task<ILockHandle?> TryAcquireAsync(
        string resource)
    {
        await auditService.LogDebugAsync($"Distributed locks are disabled. Acquiring no-op lock for resource: {resource}");
        return await Task.FromResult<ILockHandle?>(new NoOpLockHandle(resource, auditService));
    }

    public async Task<ILockHandle?> AcquireAsync(
        string resource,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        await auditService.LogDebugAsync($"Distributed locks are disabled. Acquiring no-op lock for resource: {resource}", ct);
        return await Task.FromResult<ILockHandle>(new NoOpLockHandle(resource, auditService));
    }
}

public sealed class NoOpLockHandle(string resource, IAuditService auditService) : ILockHandle
{
    private bool _released = false;

    public string Resource => resource;
    public bool IsAcquired => !_released;

    public ValueTask DisposeAsync() => new(ReleaseAsync());

    public async Task ReleaseAsync()
    {
        if (_released)
            await Task.CompletedTask;
        _released = true;
        await auditService.LogInformationAsync($"Releasing no-op lock for resource: {resource}");
        await Task.CompletedTask;
    }
}