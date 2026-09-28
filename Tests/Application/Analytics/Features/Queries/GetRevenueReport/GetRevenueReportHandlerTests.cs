using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetRevenueReport;
using Application.Analytics.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetRevenueReport;

public class GetRevenueReportHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly GetRevenueReportHandler _sut;

    public GetRevenueReportHandlerTests()
    {
        _sut = new GetRevenueReportHandler(_analytics);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceAndReturnsSuccess()
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
    }

    [Fact]
    public void Query_CacheKey_IncludesFormattedDates()
    {
        var query = new GetRevenueReportQuery(
            new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 07, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("analytics:revenue:20260701:20260731", query.CacheKey);
    }

    [Fact]
    public void Query_Expiry_IsTenMinutes()
    {
        var query = new GetRevenueReportQuery(
            new DateTime(2026, 07, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 07, 31, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(TimeSpan.FromMinutes(10), query.Expiry);
    }
}
