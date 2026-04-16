using Application.Audit.Contracts;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Search.HealthChecks;

public sealed class ElasticsearchDLQHealthCheck(
    DBContext context,
    IAuditService auditService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext healthContext,
        CancellationToken ct = default)
    {
        try
        {
            var pendingCount = await context.FailedElasticOperations
                .CountAsync(o => o.Status == "Pending", ct);

            var failedCount = await context.FailedElasticOperations
                .CountAsync(o => o.Status == "Failed", ct);

            var oldestPending = await context.FailedElasticOperations
                .Where(o => o.Status == "Pending")
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var data = new Dictionary<string, object>
            {
                ["pending_count"] = pendingCount,
                ["failed_count"] = failedCount,
                ["oldest_pending"] = oldestPending
            };

            if (failedCount > 100)
                return HealthCheckResult.Unhealthy("Too many permanently failed operations", data: data);

            if (pendingCount > 1000)
                return HealthCheckResult.Degraded("High number of pending operations", data: data);

            return HealthCheckResult.Healthy(data: data);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"DLQ health check failed: {ex.Message}", ct);
            return HealthCheckResult.Unhealthy(ex.Message, ex);
        }
    }
}