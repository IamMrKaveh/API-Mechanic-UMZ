namespace Application.Search.Features.Queries.GetSearchIndexStats;

public sealed class GetSearchIndexStatsHandler(ISearchService searchService)
        : IRequestHandler<GetSearchIndexStatsQuery, ServiceResult<SearchIndexStatsDto>>
{
    public async Task<ServiceResult<SearchIndexStatsDto>> Handle(
        GetSearchIndexStatsQuery request,
        CancellationToken ct)
    {
        var stats = await searchService.GetIndexStatsAsync(ct);

        var dto = new SearchIndexStatsDto(
            stats.ProductsCount,
            stats.CategoriesCount,
            stats.BrandsCount,
            stats.ProductsCount + stats.CategoriesCount + stats.BrandsCount);

        return ServiceResult<SearchIndexStatsDto>.Success(dto);
    }
}