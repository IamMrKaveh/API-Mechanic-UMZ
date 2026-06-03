using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Mapster;

namespace Presentation.Search.Requests;

public sealed class SearchMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetSearchSuggestionsRequest, GetSearchSuggestionsQuery>();

        config.NewConfig<FuzzySearchRequest, FuzzySearchQuery>();
    }
}