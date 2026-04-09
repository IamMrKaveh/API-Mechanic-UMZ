using Application.Search.Contracts;

namespace Application.Search.Features.Queries.GetSearchIndexStats;

public sealed class GetSearchIndexStatsHandler(ISearchService searchService)
        : IRequestHandler<GetSearchIndexStatsQuery, ServiceResult<SearchIndexStatsDto>>
{
    private readonly ISearchService _searchService = searchService;

    public async Task<ServiceResult<SearchIndexStatsDto>> Handle(
        GetSearchIndexStatsQuery request,
        CancellationToken cancellationToken)
    {
        var stats = await _searchService.GetIndexStatsAsync(cancellationToken);

        var dto = new SearchIndexStatsDto(
            stats.ProductsCount,
            stats.CategoriesCount,
            stats.BrandsCount,
            stats.ProductsCount + stats.CategoriesCount + stats.BrandsCount);

        return ServiceResult<SearchIndexStatsDto>.Success(dto);
    }
}