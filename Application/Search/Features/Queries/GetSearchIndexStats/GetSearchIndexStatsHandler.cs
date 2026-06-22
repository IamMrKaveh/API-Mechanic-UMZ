namespace Application.Search.Features.Queries.GetSearchIndexStats;

public sealed class GetSearchIndexStatsHandler(
    ISearchService searchService)
    : IQueryHandler<GetSearchIndexStatsQuery, SearchIndexStatsDto>
{
    public async Task<ServiceResult<SearchIndexStatsDto>> Handle(
        GetSearchIndexStatsQuery request,
        CancellationToken ct)
    {
        var stats = await searchService.GetIndexStatsAsync(ct);

        if (stats is null)
            return ServiceResult<SearchIndexStatsDto>.Failure("اطلاعات آماری جستجو در دسترس نیست.");

        return ServiceResult<SearchIndexStatsDto>.Success(stats);
    }
}