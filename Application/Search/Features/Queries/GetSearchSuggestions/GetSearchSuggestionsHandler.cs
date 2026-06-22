namespace Application.Search.Features.Queries.GetSearchSuggestions;

public class GetSearchSuggestionsHandler(
    ISearchService searchService)
    : IQueryHandler<GetSearchSuggestionsQuery, List<string>>
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