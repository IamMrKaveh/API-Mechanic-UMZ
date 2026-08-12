using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetCategoryPerformance;

public class GetCategoryPerformanceHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly GetCategoryPerformanceHandler _sut;

    public GetCategoryPerformanceHandlerTests()
    {
        _sut = new GetCategoryPerformanceHandler(_analytics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndDoesNotCallQueryService()
    {
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var cached = new PaginatedResult<CategoryPerformanceDto>(
            [new CategoryPerformanceDto { CategoryName = "Electronics" }], 1, 1, 10);

        _cache.GetAsync<PaginatedResult<CategoryPerformanceDto>>(
                "analytics:category-perf:20260101:20260201",
                Arg.Any<CancellationToken>())
              .Returns(cached);

        var query = new GetCategoryPerformanceQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _analytics.DidNotReceive().GetCategoryPerformanceAsync(
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<PaginatedResult<CategoryPerformanceDto>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesServiceAndCachesResultForFifteenMinutes()
    {
        var from = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 02, 01, 0, 0, 0, DateTimeKind.Utc);
        var fresh = new PaginatedResult<CategoryPerformanceDto>(
            [new CategoryPerformanceDto { CategoryName = "Books" }], 1, 1, 10);

        _analytics.GetCategoryPerformanceAsync(from, to, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetCategoryPerformanceQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetCategoryPerformanceAsync(
            from, to, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:category-perf:20260101:20260201",
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBothDatesNull_UsesEmptyDateSegmentsInCacheKey()
    {
        var fresh = new PaginatedResult<CategoryPerformanceDto>([], 0, 1, 10);

        _analytics.GetCategoryPerformanceAsync(null, null, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetCategoryPerformanceQuery(null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        await _cache.Received(1).GetAsync<PaginatedResult<CategoryPerformanceDto>>(
            "analytics:category-perf::",
            Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:category-perf::",
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }
}
