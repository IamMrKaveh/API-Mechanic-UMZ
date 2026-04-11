namespace Application.Search.Features.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsHandler(ISearchService searchService)
        : IRequestHandler<GetSearchSuggestionsQuery, ServiceResult<List<string>>>
{
    public async Task<ServiceResult<List<string>>> Handle(
        GetSearchSuggestionsQuery request, CancellationToken ct)
    {
        var suggestions = await searchService.GetSuggestionsAsync(
            request.Q,
            request.MaxSuggestions,
            ct);

        return ServiceResult<List<string>>.Success(suggestions);
    }
}