using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetDashboardStatistics;
using Application.Analytics.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetDashboardStatistics;

public class GetDashboardStatisticsHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly GetDashboardStatisticsHandler _sut;

    public GetDashboardStatisticsHandlerTests()
    {
        _sut = new GetDashboardStatisticsHandler(_analytics);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceAndReturnsSuccess()
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
    }

    [Fact]
    public void Query_CacheKey_IncludesFormattedDates()
    {
        var query = new GetDashboardStatisticsQuery(
            new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 05, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("analytics:dashboard:20260501:20260531", query.CacheKey);
    }

    [Fact]
    public void Query_CacheKey_WithBothDatesNull_UsesEmptyDateSegments()
    {
        var query = new GetDashboardStatisticsQuery(null, null);

        Assert.Equal("analytics:dashboard::", query.CacheKey);
    }

    [Fact]
    public void Query_Expiry_IsTenMinutes()
    {
        var query = new GetDashboardStatisticsQuery(null, null);

        Assert.Equal(TimeSpan.FromMinutes(10), query.Expiry);
    }
}
