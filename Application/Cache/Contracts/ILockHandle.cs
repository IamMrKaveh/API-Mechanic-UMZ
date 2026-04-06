namespace Application.Cache.Contracts;

public interface ILockHandle : IAsyncDisposable
{
    bool IsAcquired { get; }

    Task ReleaseAsync();
}