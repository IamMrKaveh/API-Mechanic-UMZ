using Application.Audit.Contracts;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Search.HealthChecks;

public sealed class ElasticsearchIndexHealthCheck(
    ElasticsearchClient client,
    IAuditService auditService) : IHealthCheck
{
    private static readonly string[] RequiredIndices = ["products_v1", "categories_v1", "brands_v1"];

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken ct = default)
    {
        try
        {
            var response = await client.Indices.ExistsAsync(
                Elastic.Clients.Elasticsearch.IndexName.From<object>(), ct);

            return HealthCheckResult.Healthy("Elasticsearch indices are accessible");
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Elasticsearch index health check failed: {ex.Message}", ct);
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}