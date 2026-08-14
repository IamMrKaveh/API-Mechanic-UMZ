using Infrastructure.Cache.Redis.Lock;

namespace Tests.Infrastructure.Cache.Redis.Lock;

public class NoOpDistributedLockTests
{
    private readonly ILogger<NoOpDistributedLock> _logger = Substitute.For<ILogger<NoOpDistributedLock>>(); private readonly NoOpDistributedLock _sut;

    public NoOpDistributedLockTests()
    {
        _sut = new NoOpDistributedLock(_logger);
    }

    [Fact]
    public async Task TryAcquireAsync_ReturnsAcquiredHandleForResource()
    {
        var handle = await _sut.TryAcquireAsync("resource-a");

        handle.ShouldNotBeNull();
        handle!.IsAcquired.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireAsync_ReturnsAcquiredHandleForResource()
    {
        var handle = await _sut.AcquireAsync("resource-b", TimeSpan.FromSeconds(30));

        handle.ShouldNotBeNull();
        handle!.IsAcquired.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireAsync_HandleReflectsResourceName()
    {
        var handle = (NoOpLockHandle)(await _sut.AcquireAsync("orders:42", TimeSpan.FromSeconds(1)))!;

        handle.Resource.ShouldBe("orders:42");
    }

    [Fact]
    public async Task ReleaseAsync_TransitionsIsAcquiredToFalse()
    {
        var handle = (await _sut.AcquireAsync("r", TimeSpan.FromSeconds(1)))!;

        await handle.ReleaseAsync();

        handle.IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_CalledTwice_IsIdempotent()
    {
        var handle = (await _sut.AcquireAsync("r", TimeSpan.FromSeconds(1)))!;

        await handle.ReleaseAsync();
        var secondCall = () => handle.ReleaseAsync();

        await secondCall.ShouldNotThrowAsync();
        handle.IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ReleasesTheHandle()
    {
        var handle = (await _sut.AcquireAsync("r", TimeSpan.FromSeconds(1)))!;

        await handle.DisposeAsync();

        handle.IsAcquired.ShouldBeFalse();
    }
}
