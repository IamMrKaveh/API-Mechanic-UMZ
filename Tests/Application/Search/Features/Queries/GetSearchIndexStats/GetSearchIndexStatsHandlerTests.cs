using Application.Search.Contracts;
using Application.Search.Features.Queries.GetSearchIndexStats;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Search.Features.Queries.GetSearchIndexStats;

public class GetSearchIndexStatsHandlerTests
{
    private readonly ISearchService _searchService = Substitute.For<ISearchService>(); private readonly GetSearchIndexStatsHandler _sut;

    public GetSearchIndexStatsHandlerTests()
    {
        _sut = new GetSearchIndexStatsHandler(_searchService);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsStats_ReturnsSuccessWithSameStats()
    {
        var expected = new SearchIndexStatsDto(1000, 20, 15, 1035);

        _searchService
            .GetIndexStatsAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetSearchIndexStatsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsNull_ReturnsFailureWithFailureErrorCode()
    {
        _searchService
            .GetIndexStatsAsync(Arg.Any<CancellationToken>())
            .Returns((SearchIndexStatsDto?)null);

        var result = await _sut.Handle(new GetSearchIndexStatsQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        result.Error.Message.ShouldBe("اطلاعات آماری جستجو در دسترس نیست.");
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToSearchService()
    {
        var expected = new SearchIndexStatsDto(0, 0, 0, 0);
        _searchService
            .GetIndexStatsAsync(Arg.Any<CancellationToken>())
            .Returns(expected);

        var ct = new CancellationTokenSource().Token;

        await _sut.Handle(new GetSearchIndexStatsQuery(), ct);

        await _searchService.Received(1).GetIndexStatsAsync(ct);
    }
}
