namespace Application.Search.Features.Queries.GetSearchIndexStats;

public class GetSearchIndexStatsHandler : IRequestHandler<GetSearchIndexStatsQuery, ServiceResult<object>>
{
    private readonly ElasticsearchClient _client;

    public GetSearchIndexStatsHandler(ElasticsearchClient client)
    {
        _client = client;
    }

    public async Task<ServiceResult<object>> Handle(GetSearchIndexStatsQuery request, CancellationToken ct)
    {
        try
        {
            var healthResponse = await _client.Cluster.HealthAsync(cancellationToken: ct);
            var statsResponse = await _client.Indices.StatsAsync(Indices.All, cancellationToken: ct);

            if (!healthResponse.IsValidResponse || !statsResponse.IsValidResponse)
                return ServiceResult<object>.Failure("خطا در دریافت وضعیت کلاستر الاستیک‌سرچ.");

            return ServiceResult<object>.Success(new
            {
                Status = healthResponse.Status.ToString(),
                TotalDocuments = statsResponse.All?.Total?.Docs?.Count ?? 0,
                ClusterName = healthResponse.ClusterName,
                NumberOfNodes = healthResponse.NumberOfNodes,
                ActivePrimaryShards = healthResponse.ActivePrimaryShards
            });
        }
        catch (Exception ex)
        {
            return ServiceResult<object>.Failure($"خطا در ارتباط با سرور جستجو: {ex.Message}");
        }
    }
}