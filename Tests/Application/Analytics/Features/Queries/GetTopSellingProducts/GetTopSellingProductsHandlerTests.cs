using Application.Analytics.Contracts;
using Application.Analytics.Features.Queries.GetTopSellingProducts;
using Application.Analytics.Features.Shared;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Analytics.Features.Queries.GetTopSellingProducts;

public class GetTopSellingProductsHandlerTests
{
    private readonly IAnalyticsQueryService _analytics = Substitute.For<IAnalyticsQueryService>(); private readonly GetTopSellingProductsHandler _sut;

    public GetTopSellingProductsHandlerTests()
    {
        _sut = new GetTopSellingProductsHandler(_analytics);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceAndReturnsSuccess()
    {
        var from = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 04, 30, 0, 0, 0, DateTimeKind.Utc);
        const int count = 5;
        var fresh = new PaginatedResult<TopSellingProductDto>(
            [new TopSellingProductDto { ProductId = Guid.NewGuid(), ProductName = "Alpha" }], 1, 1, 10);

        _analytics.GetTopSellingProductsAsync(count, from, to, Arg.Any<CancellationToken>())
                  .Returns(fresh);

        var query = new GetTopSellingProductsQuery(count, from, to);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(fresh);

        await _analytics.Received(1).GetTopSellingProductsAsync(
            count, from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Query_CacheKey_IncludesCountAndFormattedDates()
    {
        var query = new GetTopSellingProductsQuery(
            5,
            new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 04, 30, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal("analytics:top-products:5:20260401:20260430", query.CacheKey);
    }

    [Fact]
    public void Query_CacheKey_WithBothDatesNull_UsesEmptyDateSegments()
    {
        var query = new GetTopSellingProductsQuery(10, null, null);

        Assert.Equal("analytics:top-products:10::", query.CacheKey);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(100)]
    public void Query_CacheKey_IncludesCountVerbatim(int count)
    {
        var query = new GetTopSellingProductsQuery(count, null, null);

        Assert.Equal($"analytics:top-products:{count}::", query.CacheKey);
    }

    [Fact]
    public void Query_Expiry_IsFifteenMinutes()
    {
        var query = new GetTopSellingProductsQuery();

        Assert.Equal(TimeSpan.FromMinutes(15), query.Expiry);
    }
}
