using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetInventoryReport;
using Application.Analytics.Features.Shared;
using Application.Cache.Contracts;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetInventoryReport;

public class GetInventoryReportHandlerTests
{
    private const string ExpectedCacheKey = "analytics:inventory-report";

    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly GetInventoryReportHandler _sut;

    public GetInventoryReportHandlerTests()
    {
        _sut = new GetInventoryReportHandler(_analytics, _cache);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsCachedValueAndDoesNotCallQueryService()
    {
        var cached = new InventoryReportDto { TotalVariants = 500, InStockVariants = 400 };

        _cache.GetAsync<InventoryReportDto>(ExpectedCacheKey, Arg.Any<CancellationToken>())
              .Returns(cached);

        var result = await _sut.Handle(new GetInventoryReportQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);

        await _analytics.DidNotReceive().GetInventoryReportAsync(Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<InventoryReportDto>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_QueriesServiceAndCachesResultForFiveMinutes()
    {
        var fresh = new InventoryReportDto { TotalVariants = 123 };

        _analytics.GetInventoryReportAsync(Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var result = await _sut.Handle(new GetInventoryReportQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetInventoryReportAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).GetAsync<InventoryReportDto>(
            ExpectedCacheKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            ExpectedCacheKey,
            fresh,
            TimeSpan.FromMinutes(5),
            Arg.Any<CancellationToken>());
    }
}
