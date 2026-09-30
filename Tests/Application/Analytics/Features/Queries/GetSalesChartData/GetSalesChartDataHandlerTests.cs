using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetSalesChartData;
using Application.Analytics.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetSalesChartData;

public class GetSalesChartDataHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly GetSalesChartDataHandler _sut;

    public GetSalesChartDataHandlerTests()
    {
        _sut = new GetSalesChartDataHandler(_analytics);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceWithGroupByAndReturnsSuccess()
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
    }

}
