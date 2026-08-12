using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Behaviors;
using MediatR;

namespace Tests.Application.Common.Behaviors;

public class CachingBehaviorTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly IAuditService _audit = Substitute.For<IAuditService>();

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_CallsNextAndSkipsCache()
    {
        var sut = new CachingBehavior<NonCacheableRequest, string>(_cache, _audit);
        var invoked = false;

        var result = await sut.Handle(
            new NonCacheableRequest(),
            _ =>
            {
                invoked = true;
                return Task.FromResult("value");
            },
            CancellationToken.None);

        invoked.ShouldBeTrue();
        result.ShouldBe("value");
        await _cache.DidNotReceiveWithAnyArgs().GetAsync<string>(default!, default);
        await _cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default(string)!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndSkipsNext()
    {
        var expiry = TimeSpan.FromMinutes(5);
        var request = new CacheableTestQuery("k:hit", expiry);

        _cache.GetAsync<string>("k:hit", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("cached"));

        var sut = new CachingBehavior<CacheableTestQuery, string>(_cache, _audit);
        var invoked = false;

        var result = await sut.Handle(
            request,
            _ =>
            {
                invoked = true;
                return Task.FromResult("fresh");
            },
            CancellationToken.None);

        invoked.ShouldBeFalse();
        result.ShouldBe("cached");
        await _audit.Received(1).LogSystemEventAsync(
            "Cache hit",
            Arg.Is<string>(s => s.Contains("k:hit")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_CallsNextAndStoresResult()
    {
        var expiry = TimeSpan.FromMinutes(1);
        var request = new CacheableTestQuery("k:miss", expiry);

        _cache.GetAsync<string>("k:miss", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        var sut = new CachingBehavior<CacheableTestQuery, string>(_cache, _audit);

        var result = await sut.Handle(
            request,
            _ => Task.FromResult("fresh"),
            CancellationToken.None);

        result.ShouldBe("fresh");
        await _cache.Received(1).SetAsync(
            "k:miss",
            "fresh",
            expiry,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndResponseIsNull_DoesNotWriteCache()
    {
        var request = new CacheableTestQuery("k:null", null);

        _cache.GetAsync<string?>("k:null", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        var sut = new CachingBehavior<CacheableTestQuery, string?>(_cache, _audit);

        var result = await sut.Handle(
            request,
            _ => Task.FromResult<string?>(null),
            CancellationToken.None);

        result.ShouldBeNull();
        await _cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default(string?)!, default, default);
    }

    public sealed record CacheableTestQuery(string CacheKey, TimeSpan? Expiry)
        : IRequest<string>, ICacheableQuery;

    public sealed record NonCacheableRequest : IRequest<string>;
}
