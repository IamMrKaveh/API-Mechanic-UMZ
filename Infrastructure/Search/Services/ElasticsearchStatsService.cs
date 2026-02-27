namespace Infrastructure.Search.Services;

public class ElasticsearchStatsService : ISearchStatsService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchStatsService> _logger;

    public ElasticsearchStatsService(
        ElasticsearchClient client,
        ILogger<ElasticsearchStatsService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<SearchStatsResult> GetStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var healthResponse = await _client.Cluster.HealthAsync(cancellationToken: ct);
            var statsResponse = await _client.Indices.StatsAsync(Indices.All, cancellationToken: ct);

            if (!healthResponse.IsValidResponse || !statsResponse.IsValidResponse)
                return new SearchStatsResult(false, UnavailableReason: "خطا در دریافت وضعیت کلاستر الاستیک‌سرچ.");

            return new SearchStatsResult(
                IsAvailable: true,
                Status: healthResponse.Status.ToString(),
                TotalDocuments: statsResponse.All?.Total?.Docs?.Count ?? 0,
                ClusterName: healthResponse.ClusterName,
                NumberOfNodes: healthResponse.NumberOfNodes,
                ActivePrimaryShards: healthResponse.ActivePrimaryShards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت آمار Elasticsearch");
            return new SearchStatsResult(false, UnavailableReason: $"خطا در ارتباط با سرور جستجو: {ex.Message}");
        }
    }
}