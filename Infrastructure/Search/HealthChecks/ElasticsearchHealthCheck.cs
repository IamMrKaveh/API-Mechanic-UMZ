namespace Infrastructure.Search.HealthChecks;

public class ElasticsearchHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchHealthCheck> _logger;

    public ElasticsearchHealthCheck(
        ElasticsearchClient client,
        ILogger<ElasticsearchHealthCheck> logger)
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
            var pingResponse = await _client.PingAsync(cancellationToken: cancellationToken);

            if (!pingResponse.IsValidResponse)
            {
                _logger.LogError("Elasticsearch ping failed: {Error}", pingResponse.DebugInformation);
                return HealthCheckResult.Unhealthy(
                    "Elasticsearch ping failed",
                    exception: null,
                    data: new Dictionary<string, object>
                    {
                        { "error", pingResponse.DebugInformation ?? "Unknown error" }
                    });
            }

            var healthResponse = await _client.Cluster.HealthAsync(cancellationToken: cancellationToken);

            if (!healthResponse.IsValidResponse)
            {
                return HealthCheckResult.Degraded(
                    "Elasticsearch is responding but cluster health check failed");
            }

            var clusterStatus = healthResponse.Status.ToString().ToLower();
            var data = new Dictionary<string, object>
            {
                { "cluster_name", healthResponse.ClusterName ?? "unknown" },
                { "status", clusterStatus },
                { "number_of_nodes", healthResponse.NumberOfNodes.ToString() },
                { "number_of_data_nodes", healthResponse.NumberOfDataNodes.ToString() },
                { "active_primary_shards", healthResponse.ActivePrimaryShards.ToString() },
                { "active_shards", healthResponse.ActiveShards.ToString() },
                { "relocating_shards", healthResponse.RelocatingShards.ToString() },
                { "initializing_shards", healthResponse.InitializingShards.ToString() },
                { "unassigned_shards", healthResponse.UnassignedShards.ToString() }
            };

            return clusterStatus switch
            {
                "green" => HealthCheckResult.Healthy(
                    "Elasticsearch cluster is healthy", data),
                "yellow" => HealthCheckResult.Degraded(
                    "Elasticsearch cluster status is yellow", null, data),
                "red" => HealthCheckResult.Unhealthy(
                    "Elasticsearch cluster status is red", null, data),
                _ => HealthCheckResult.Unhealthy(
                    $"Unknown Elasticsearch cluster status: {clusterStatus}", null, data)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while checking Elasticsearch health");
            return HealthCheckResult.Unhealthy(
                "Exception occurred while checking Elasticsearch",
                exception: ex);
        }
    }
}

public class ElasticsearchIndexHealthCheck : IHealthCheck
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchIndexHealthCheck> _logger;
    private readonly string[] _requiredIndices = { "products_v1", "categories_v1", "categorygroups_v1" };

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