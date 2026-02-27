namespace Infrastructure.Search.HealthChecks;

public class ElasticsearchIndexHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchIndexHealthCheck> _logger;
    private readonly string[] _requiredIndices = { "products_v1", "categories_v1", "brands_v1" };

    public ElasticsearchIndexHealthCheck(
        ElasticsearchClient client,
        ILogger<ElasticsearchIndexHealthCheck> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
    HealthCheckContext context,
    CancellationToken cancellationToken = default)
    {
        try
        {
            var missingIndices = new List<string>();
            var indexStats = new Dictionary<string, object>();

            foreach (var indexName in _requiredIndices)
            {
                var existsResponse = await _client.Indices.ExistsAsync(
                    indexName,
                    cancellationToken: cancellationToken);

                if (!existsResponse.Exists)
                {
                    missingIndices.Add(indexName);
                    continue;
                }

                var statsResponse = await _client.Indices.StatsAsync(
                    indices: indexName,
                    cancellationToken: cancellationToken);

                if (statsResponse.IsValidResponse &&
                    statsResponse.Indices != null &&
                    statsResponse.Indices.TryGetValue(indexName, out var stats))
                {
                    indexStats[indexName] = new
                    {
                        docs_count = stats.Total?.Docs?.Count ?? 0,
                        store_size = stats.Total?.Store?.SizeInBytes ?? 0,
                        segments_count = stats.Total?.Segments?.Count ?? 0
                    };
                }
            }

            if (missingIndices.Any())
            {
                return HealthCheckResult.Unhealthy(
                    $"Required indices are missing: {string.Join(", ", missingIndices)}",
                    data: new Dictionary<string, object>
                    {
                        ["missing_indices"] = missingIndices,
                        ["existing_indices"] = indexStats
                    });
            }

            return HealthCheckResult.Healthy(
                "All required Elasticsearch indices exist",
                data: indexStats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while checking Elasticsearch indices health");

            return HealthCheckResult.Unhealthy(
                "Exception occurred while checking indices",
                exception: ex);
        }
    }
}