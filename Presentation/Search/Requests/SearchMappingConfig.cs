using Application.Search.Features.Queries.FuzzySearch;
using Application.Search.Features.Queries.GetSearchSuggestions;
using Application.Search.Features.Queries.SearchProducts;
using Mapster;
using Presentation.Search.Requests;

namespace Presentation.Search.Mapping;

public sealed class SearchMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SearchProductsRequest, SearchProductsQuery>()
            .Map(dest => dest.Tags, src => src.Tags != null ? src.Tags.ToList() : null);

        config.NewConfig<GetSearchSuggestionsRequest, GetSearchSuggestionsQuery>();

        config.NewConfig<FuzzySearchRequest, FuzzySearchQuery>();
    }
}