namespace Tests.TestInfrastructure.Fakes;

public sealed class FakeLockHandle : ILockHandle
{
    public FakeLockHandle(string resource, bool isAcquired)
    {
        Resource = resource;
        IsAcquired = isAcquired;
    }

    public string Resource { get; }

    public bool IsAcquired { get; }

    public Task ReleaseAsync() => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
