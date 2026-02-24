namespace Application.Cache.Contracts;

public interface ILockHandle : IAsyncDisposable
{
    string Resource { get; }
    bool IsAcquired { get; }

    Task ReleaseAsync();
}