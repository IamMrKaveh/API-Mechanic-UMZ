namespace Application.Cache.Contracts;

public interface IDistributedLock
{
    Task<ILockHandle?> AcquireAsync(string resource, TimeSpan expiry, CancellationToken ct = default);
}