using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetSalesChartData;

public class GetSalesChartDataHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly GetSalesChartDataHandler _sut;

    public GetSalesChartDataHandlerTests()
    {
        _sut = new GetSalesChartDataHandler(_analytics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndDoesNotCallQueryService()
    {
        var from = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 06, 30, 0, 0, 0, DateTimeKind.Utc);
        var cached = new PaginatedResult<SalesChartDataPointDto>(
            [new SalesChartDataPointDto { Label = "2026-06-01", OrderCount = 10 }], 1, 1, 10);

        _cache.GetAsync<PaginatedResult<SalesChartDataPointDto>>(
                "analytics:sales-chart:20260601:20260630:day",
                Arg.Any<CancellationToken>())
              .Returns(cached);

        var query = new GetSalesChartDataQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _analytics.DidNotReceive().GetSalesChartDataAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<PaginatedResult<SalesChartDataPointDto>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesServiceWithGroupByAndCachesResultForFifteenMinutes()
    {
        var from = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 06, 30, 0, 0, 0, DateTimeKind.Utc);
        const string groupBy = "week";
        var fresh = new PaginatedResult<SalesChartDataPointDto>(
            [new SalesChartDataPointDto { Label = "W23", OrderCount = 5 }], 1, 1, 10);

        _analytics.GetSalesChartDataAsync(from, to, groupBy, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetSalesChartDataQuery(from, to, groupBy);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetSalesChartDataAsync(
            from, to, groupBy, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:sales-chart:20260601:20260630:week",
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    public async Task Handle_CacheKeyIncludesGroupByVerbatim(string groupBy)
    {
        var from = new DateTime(2026, 06, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 06, 30, 0, 0, 0, DateTimeKind.Utc);
        var fresh = new PaginatedResult<SalesChartDataPointDto>([], 0, 1, 10);

        _analytics.GetSalesChartDataAsync(from, to, groupBy, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetSalesChartDataQuery(from, to, groupBy);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        var expectedKey = $"analytics:sales-chart:20260601:20260630:{groupBy}";

        await _cache.Received(1).GetAsync<PaginatedResult<SalesChartDataPointDto>>(
            expectedKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            expectedKey,
            fresh,
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }
}
