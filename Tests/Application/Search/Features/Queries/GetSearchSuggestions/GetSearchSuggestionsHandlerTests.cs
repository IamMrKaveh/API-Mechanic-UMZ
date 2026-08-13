using Application.Search.Contracts;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Search.Features.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsHandlerTests
{
    private readonly ISearchService _searchService = Substitute.For<ISearchService>(); private readonly GetSearchSuggestionsHandler _sut;

    public GetSearchSuggestionsHandlerTests()
    {
        _sut = new GetSearchSuggestionsHandler(_searchService);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsSuggestions_ReturnsSuccessWithSameSuggestions()
    {
        var expected = new List<string> { "laptop", "laptop stand", "laptop bag" };

        _searchService
            .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetSearchSuggestionsQuery("lap", 10), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Handle_ForwardsQueryParametersToSearchService()
    {
        _searchService
            .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var ct = new CancellationTokenSource().Token;
        var query = new GetSearchSuggestionsQuery("sho", 15);

        await _sut.Handle(query, ct);

        await _searchService.Received(1).GetSuggestionsAsync("sho", 15, ct);
    }

    [Fact]
    public async Task Handle_WhenSearchServiceReturnsEmpty_ReturnsSuccessWithEmptyList()
    {
        _searchService
            .GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<string>());

        var result = await _sut.Handle(new GetSearchSuggestionsQuery("xyz", 5), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
