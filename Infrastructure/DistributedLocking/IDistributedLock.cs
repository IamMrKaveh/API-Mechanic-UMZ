namespace Infrastructure.DistributedLocking;

public interface IDistributedLock
{
    Task<ILockHandle?> AcquireAsync(
        string key,
        TimeSpan expiry,
        CancellationToken ct = default);
}
