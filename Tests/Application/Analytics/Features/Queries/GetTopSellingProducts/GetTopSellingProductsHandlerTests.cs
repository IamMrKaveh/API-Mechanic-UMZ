using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetTopSellingProducts;

public class GetTopSellingProductsHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly GetTopSellingProductsHandler _sut;

    public GetTopSellingProductsHandlerTests()
    {
        _sut = new GetTopSellingProductsHandler(_analytics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndDoesNotCallQueryService()
    {
        var from = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 04, 30, 0, 0, 0, DateTimeKind.Utc);
        const int count = 20;
        var cached = new PaginatedResult<TopSellingProductDto>(
            [new TopSellingProductDto { ProductId = Guid.NewGuid(), ProductName = "Widget" }], 1, 1, 10);

        _cache.GetAsync<PaginatedResult<TopSellingProductDto>>(
                "analytics:top-products:20:20260401:20260430",
                Arg.Any<CancellationToken>())
              .Returns(cached);

        var query = new GetTopSellingProductsQuery(count, from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _analytics.DidNotReceive().GetTopSellingProductsAsync(
            Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<PaginatedResult<TopSellingProductDto>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesServiceAndCachesResultForFifteenMinutes()
    {
        var from = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 04, 30, 0, 0, 0, DateTimeKind.Utc);
        const int count = 5;
        var fresh = new PaginatedResult<TopSellingProductDto>(
            [new TopSellingProductDto { ProductId = Guid.NewGuid(), ProductName = "Alpha" }], 1, 1, 10);

        _analytics.GetTopSellingProductsAsync(count, from, to, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetTopSellingProductsQuery(count, from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetTopSellingProductsAsync(
            count, from, to, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:top-products:5:20260401:20260430",
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBothDatesNull_UsesEmptyDateSegmentsInCacheKey()
    {
        const int count = 10;
        var fresh = new PaginatedResult<TopSellingProductDto>([], 0, 1, 10);

        _analytics.GetTopSellingProductsAsync(count, null, null, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetTopSellingProductsQuery(count, null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        await _cache.Received(1).GetAsync<PaginatedResult<TopSellingProductDto>>(
            "analytics:top-products:10::",
            Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:top-products:10::",
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(100)]
    public async Task Handle_CacheKeyIncludesCountVerbatim(int count)
    {
        var fresh = new PaginatedResult<TopSellingProductDto>([], 0, 1, 10);

        _analytics.GetTopSellingProductsAsync(count, null, null, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetTopSellingProductsQuery(count, null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        var expectedKey = $"analytics:top-products:{count}::";

        await _cache.Received(1).GetAsync<PaginatedResult<TopSellingProductDto>>(
            expectedKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            expectedKey,
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }
}
