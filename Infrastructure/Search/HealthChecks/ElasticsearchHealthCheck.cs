using Infrastructure.Search.Options;

namespace Infrastructure.Search.HealthChecks;

public sealed class ElasticsearchHealthCheck(
    ElasticsearchClient client,
    IOptions<ElasticsearchOptions> options,
    IAuditService auditService) : IHealthCheck
{
    private readonly ElasticsearchOptions _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken ct = default)
    {
        try
        {
            var response = await client.PingAsync(ct);

            return response.IsValidResponse
                ? HealthCheckResult.Healthy("Elasticsearch is reachable")
                : HealthCheckResult.Unhealthy("Elasticsearch ping failed");
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Elasticsearch health check failed: {ex.Message}", ct);
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}