namespace Infrastructure.Search.HealthChecks;

public class ElasticsearchHealthCheck(
    ElasticsearchClient client,
    ILogger<ElasticsearchHealthCheck> logger,
    IOptions<ElasticsearchOptions> options) : IHealthCheck
{
    private readonly ElasticsearchClient _client = client;
    private readonly ILogger<ElasticsearchHealthCheck> _logger = logger;
    private readonly ElasticsearchOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        // اگر Elasticsearch غیرفعال است، Healthy برگردان
        if (!_options.IsEnabled)
        {
            return HealthCheckResult.Healthy("Elasticsearch is disabled in configuration");
        }

        try
        {
            var pingResponse = await _client.PingAsync(cancellationToken: ct);
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

            var healthResponse = await _client.Cluster.HealthAsync(cancellationToken: ct);
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
                "green" => HealthCheckResult.Healthy("Elasticsearch cluster is healthy", data),
                "yellow" => HealthCheckResult.Degraded("Elasticsearch cluster status is yellow", null, data),
                "red" => HealthCheckResult.Unhealthy("Elasticsearch cluster status is red", null, data),
                _ => HealthCheckResult.Unhealthy($"Unknown Elasticsearch cluster status: {clusterStatus}", null, data)
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