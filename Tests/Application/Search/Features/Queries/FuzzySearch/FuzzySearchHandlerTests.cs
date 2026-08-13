using Application.Search.Contracts;
using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Search.Features.Queries.FuzzySearch;

public class FuzzySearchHandlerTests
{
    private readonly ISearchService _searchService = Substitute.For<ISearchService>(); private readonly FuzzySearchHandler _sut;

    public FuzzySearchHandlerTests()
    {
        _sut = new FuzzySearchHandler(_searchService);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsResults_ReturnsSuccessWithSameResult()
    {
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>
        {
            new() { ProductId = Guid.NewGuid(), Name = "laptop" }
        },
            Total = 1,
            Page = 1,
            PageSize = 10
        };

        _searchService
            .SearchWithFuzzyAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new FuzzySearchQuery("laptp", 1, 10), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsQueryParametersToSearchService()
    {
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>(),
            Total = 0,
            Page = 3,
            PageSize = 25
        };

        _searchService
            .SearchWithFuzzyAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var ct = new CancellationTokenSource().Token;
        var query = new FuzzySearchQuery("shooes", 3, 25);

        await _sut.Handle(query, ct);

        await _searchService.Received(1).SearchWithFuzzyAsync("shooes", 3, 25, ct);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsEmpty_ReturnsSuccessWithEmptyItems()
    {
        var expected = new SearchResultDto<ProductSearchResultItemDto>
        {
            Items = new List<ProductSearchResultItemDto>(),
            Total = 0,
            Page = 1,
            PageSize = 10
        };

        _searchService
            .SearchWithFuzzyAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new FuzzySearchQuery("xyz", 1, 10), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.ShouldBeEmpty();
        result.Value.Total.ShouldBe(0);
    }
}
