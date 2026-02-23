namespace Application.Cache.Contracts;

public interface IDistributedLock
{
    Task<ILockHandle?> TryAcquireAsync(
        string resource,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int retryCount = 5,
        CancellationToken ct = default
        );

    Task<ILockHandle> AcquireAsync(
        string resource,
        TimeSpan? ttl = null,
        TimeSpan? retryDelay = null,
        int retryCount = 5,
        CancellationToken ct = default
        );
}