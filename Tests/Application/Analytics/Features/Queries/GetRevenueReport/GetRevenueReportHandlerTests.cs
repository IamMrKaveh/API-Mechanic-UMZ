using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetRevenueReport;

public class GetRevenueReportHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly GetRevenueReportHandler _sut;

    public GetRevenueReportHandlerTests()
    {
        _sut = new GetRevenueReportHandler(_analytics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndDoesNotCallQueryService()
    {
        var from = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 07, 31, 0, 0, 0, DateTimeKind.Utc);
        var cached = new RevenueReportDto
        {
            FromDate = from,
            ToDate = to,
            GrossRevenue = 100_000m,
            NetRevenue = 90_000m
        };

        _cache.GetAsync<RevenueReportDto>(
                "analytics:revenue:20260701:20260731",
                Arg.Any<CancellationToken>())
              .Returns(cached);

        var query = new GetRevenueReportQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _analytics.DidNotReceive().GetRevenueReportAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<RevenueReportDto>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesServiceAndCachesResultForTenMinutes()
    {
        var from = new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 07, 31, 0, 0, 0, DateTimeKind.Utc);
        var fresh = new RevenueReportDto
        {
            FromDate = from,
            ToDate = to,
            GrossRevenue = 250_000m
        };

        _analytics.GetRevenueReportAsync(from, to, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetRevenueReportQuery(from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetRevenueReportAsync(
            from, to, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            "analytics:revenue:20260701:20260731",
            fresh,
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
