using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Mapster;
using Presentation.Search.Requests;

namespace Tests.Presentation.Search.Requests;

public class SearchMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly SearchMappingConfig _sut = new();

    public SearchMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetSearchSuggestionsRequest_MapsToQuery()
    {
        // Arrange
        var request = new GetSearchSuggestionsRequest("pho", 5);

        // Act
        var query = request.Adapt<GetSearchSuggestionsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Q.ShouldBe(request.Q);
        query.MaxSuggestions.ShouldBe(request.MaxSuggestions);
    }

    [Fact]
    public void FuzzySearchRequest_MapsToQuery()
    {
        // Arrange
        var request = new FuzzySearchRequest("phone", 2, 15);

        // Act
        var query = request.Adapt<FuzzySearchQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Q.ShouldBe(request.Q);
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
    }

    [Fact]
    public void SearchMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
