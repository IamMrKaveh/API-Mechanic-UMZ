namespace Application.Search.Features.Queries.GetSearchIndexStats;

public class GetSearchIndexStatsHandler : IRequestHandler<GetSearchIndexStatsQuery, ServiceResult<object>>
{
    public Task<ServiceResult<object>> Handle(GetSearchIndexStatsQuery request, CancellationToken ct)
    {
        // Dummy implementation, map to actual Elastic client cluster stats in real life
        return Task.FromResult(ServiceResult<object>.Success(new { Status = "Green", TotalDocuments = 1500 }));
    }
}