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

}
