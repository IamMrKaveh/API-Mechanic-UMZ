namespace Application.Search.Features.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsHandler
    : IRequestHandler<GetSearchSuggestionsQuery, ServiceResult<List<string>>>
{
    private readonly ISearchService _searchService;

    public GetSearchSuggestionsHandler(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public async Task<ServiceResult<List<string>>> Handle(
        GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var suggestions = await _searchService.GetSuggestionsAsync(
            request.Q, request.MaxSuggestions, cancellationToken);

        return ServiceResult<List<string>>.Success(suggestions);
    }
}