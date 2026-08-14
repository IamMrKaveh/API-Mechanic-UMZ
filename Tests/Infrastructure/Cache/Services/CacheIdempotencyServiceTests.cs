using Application.Cache.Contracts;
using Infrastructure.Cache.Services;

namespace Tests.Infrastructure.Cache.Services;

public class CacheIdempotencyServiceTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly CacheIdempotencyService _sut;

    public CacheIdempotencyServiceTests()
    {
        _sut = new CacheIdempotencyService(_cache);
    }

    private static string ExpectedKey(Guid id) => $"idempotency:{id:N}";

    [Fact]
    public async Task HasBeenProcessedAsync_WhenIdempotencyKeyIsEmpty_ReturnsFalseAndDoesNotQueryCache()
    {
        var result = await _sut.HasBeenProcessedAsync(Guid.Empty, CancellationToken.None);

        result.ShouldBeFalse();
        await _cache.DidNotReceiveWithAnyArgs().GetAsync<string>(default!, default);
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenCacheReturnsValue_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _cache.GetAsync<string>(ExpectedKey(id), Arg.Any<CancellationToken>())
            .Returns("stored-payload");

        var result = await _sut.HasBeenProcessedAsync(id, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenCacheReturnsNull_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        _cache.GetAsync<string>(ExpectedKey(id), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.HasBeenProcessedAsync(id, CancellationToken.None);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WhenIdempotencyKeyIsEmpty_DoesNotWriteToCache()
    {
        await _sut.MarkAsProcessedAsync(Guid.Empty, "payload", CancellationToken.None);

        await _cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default(string)!, default, default);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WithValidKey_StoresResultUsingNamespacedKeyAndTwentyFourHourExpiry()
    {
        var id = Guid.NewGuid();
        const string payload = "result-json";

        await _sut.MarkAsProcessedAsync(id, payload, CancellationToken.None);

        await _cache.Received(1).SetAsync(
            ExpectedKey(id),
            payload,
            TimeSpan.FromHours(24),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WhenResultIsNull_StoresEmptyStringInsteadOfNull()
    {
        var id = Guid.NewGuid();

        await _sut.MarkAsProcessedAsync(id, null!, CancellationToken.None);

        await _cache.Received(1).SetAsync(
            ExpectedKey(id),
            string.Empty,
            TimeSpan.FromHours(24),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetResultAsync_WhenIdempotencyKeyIsEmpty_ReturnsNullAndDoesNotQueryCache()
    {
        var result = await _sut.GetResultAsync(Guid.Empty, CancellationToken.None);

        result.ShouldBeNull();
        await _cache.DidNotReceiveWithAnyArgs().GetAsync<string>(default!, default);
    }

    [Fact]
    public async Task GetResultAsync_WhenValueExists_ReturnsStoredResult()
    {
        var id = Guid.NewGuid();
        _cache.GetAsync<string>(ExpectedKey(id), Arg.Any<CancellationToken>())
            .Returns("cached-result");

        var result = await _sut.GetResultAsync(id, CancellationToken.None);

        result.ShouldBe("cached-result");
    }

    [Fact]
    public async Task GetResultAsync_WhenNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _cache.GetAsync<string>(ExpectedKey(id), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await _sut.GetResultAsync(id, CancellationToken.None);

        result.ShouldBeNull();
    }
}
