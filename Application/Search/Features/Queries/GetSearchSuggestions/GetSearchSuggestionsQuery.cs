namespace Application.Search.Features.Queries.GetSearchSuggestions;

public record GetSearchSuggestionsQuery(string Q, int MaxSuggestions = 10)
    : IRequest<ServiceResult<List<string>>>;