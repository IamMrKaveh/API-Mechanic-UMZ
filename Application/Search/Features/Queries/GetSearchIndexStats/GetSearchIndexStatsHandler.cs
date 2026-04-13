using Application.Search.Contracts;
using Application.Search.Features.Shared;

namespace Application.Search.Features.Queries.GetSearchIndexStats;

public sealed class GetSearchIndexStatsHandler(ISearchService searchService)
    : IRequestHandler<GetSearchIndexStatsQuery, ServiceResult<SearchIndexStatsDto>>
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