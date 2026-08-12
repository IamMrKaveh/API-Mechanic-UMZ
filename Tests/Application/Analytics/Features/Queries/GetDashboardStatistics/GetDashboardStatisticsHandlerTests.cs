using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetDashboardStatistics;

public class GetDashboardStatisticsHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly GetDashboardStatisticsHandler _sut;

    public GetDashboardStatisticsHandlerTests()
    {
        _sut = new GetDashboardStatisticsHandler(_analytics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndDoesNotCallQueryService()
    {
        var from = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 05, 31, 0, 0, 0, DateTimeKind.Utc);
        var cached = new DashboardStatisticsDto { TotalOrders = 123, TotalRevenue = 4567m };

        _cache.GetAsync<DashboardStatisticsDto>(
                "analytics:dashboard:20260501:20260531",
                Arg.Any<CancellationToken>())
              .Returns(cached);

        var query = new GetDashboardStatisticsQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _analytics.DidNotReceive().GetDashboardStatisticsAsync(
            Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<DashboardStatisticsDto>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesServiceAndCachesResultForTenMinutes()
    {
        var from = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 05, 31, 0, 0, 0, DateTimeKind.Utc);
        var fresh = new DashboardStatisticsDto { TotalOrders = 42 };

        _analytics.GetDashboardStatisticsAsync(from, to, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetDashboardStatisticsQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetDashboardStatisticsAsync(
            from, to, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:dashboard:20260501:20260531",
            fresh,
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithBothDatesNull_UsesEmptyDateSegmentsInCacheKey()
    {
        var fresh = new DashboardStatisticsDto();

        _analytics.GetDashboardStatisticsAsync(null, null, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetDashboardStatisticsQuery(null, null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        await _cache.Received(1).GetAsync<DashboardStatisticsDto>(
            "analytics:dashboard::",
            Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:dashboard::",
            fresh,
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
