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
        var missing = new List<string>();
        var failed = new List<string>();

        foreach (var index in RequiredIndices)
        {
            try
            {
                var response = await client.Indices.ExistsAsync(index, ct);

                if (!response.Exists)
                    missing.Add(index);
            }
            catch (Exception ex)
            {
                failed.Add($"{index}: {ex.Message}");
                await auditService.LogErrorAsync(
                    $"Elasticsearch index existence check failed for '{index}': {ex.Message}", ct);
            }
        }

        if (failed.Count > 0)
        {
            return HealthCheckResult.Unhealthy(
                $"Elasticsearch connectivity failures: {string.Join(", ", failed)}");
        }

        if (missing.Count == RequiredIndices.Length)
        {
            return HealthCheckResult.Unhealthy(
                $"All required indices are missing: {string.Join(", ", missing)}");
        }

        if (missing.Count > 0)
        {
            return HealthCheckResult.Degraded(
                $"Missing required indices: {string.Join(", ", missing)}");
        }

        return HealthCheckResult.Healthy(
            $"All required indices present: {string.Join(", ", RequiredIndices)}");
    }
}