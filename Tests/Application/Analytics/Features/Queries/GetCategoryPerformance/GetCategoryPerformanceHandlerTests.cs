using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetCategoryPerformance;
using Application.Analytics.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetCategoryPerformance;

public class GetCategoryPerformanceHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly GetCategoryPerformanceHandler _sut;

    public GetCategoryPerformanceHandlerTests()
    {
        _sut = new GetCategoryPerformanceHandler(_analytics);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceAndReturnsSuccess()
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
    }

}
