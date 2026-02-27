namespace Application.Search.Features.Queries.GetSearchIndexStats;

public class GetSearchIndexStatsHandler : IRequestHandler<GetSearchIndexStatsQuery, ServiceResult<object>>
{
    private readonly ISearchStatsService _statsService;

    public GetSearchIndexStatsHandler(ISearchStatsService statsService)
    {
        _statsService = statsService;
    }

    public async Task<ServiceResult<object>> Handle(GetSearchIndexStatsQuery request, CancellationToken ct)
    {
        var result = await _statsService.GetStatsAsync(ct);

        if (!result.IsAvailable)
            return ServiceResult<object>.Failure(result.UnavailableReason ?? "سرویس جستجو غیرفعال است.");

        return ServiceResult<object>.Success(new
        {
            result.Status,
            result.TotalDocuments,
            result.ClusterName,
            result.NumberOfNodes,
            result.ActivePrimaryShards
        });
    }
}