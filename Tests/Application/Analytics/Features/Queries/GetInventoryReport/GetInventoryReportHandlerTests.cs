using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetInventoryReport;
using Application.Analytics.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetInventoryReport;

public class GetInventoryReportHandlerTests
{
    private const string ExpectedCacheKey = "analytics:inventory-report";

    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>();
    private readonly GetInventoryReportHandler _sut;

    public GetInventoryReportHandlerTests()
    {
        _sut = new GetInventoryReportHandler(_analytics);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceAndReturnsSuccess()
    {
        var fresh = new InventoryReportDto { TotalVariants = 123 };

        _analytics.GetInventoryReportAsync(Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var result = await _sut.Handle(new GetInventoryReportQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetInventoryReportAsync(Arg.Any<CancellationToken>());
    }

}
