namespace Infrastructure.Search.HealthChecks;

public class ElasticsearchDLQHealthCheck(
    DBContext context,
    ILogger<ElasticsearchDLQHealthCheck> logger,
    IConfiguration configuration) : IHealthCheck
{
    private readonly DBContext _context = context;
    private readonly ILogger<ElasticsearchDLQHealthCheck> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var warningThreshold = _configuration.GetValue("Elasticsearch:DLQWarningThreshold", 100);
            var criticalThreshold = _configuration.GetValue("Elasticsearch:DLQCriticalThreshold", 500);

            var pendingCount = await _context.FailedElasticOperations
                .CountAsync(o => o.Status == "Pending" && o.RetryCount < 5, ct);

            var failedCount = await _context.FailedElasticOperations
                .CountAsync(o => o.RetryCount >= 5, ct);

            var oldestPending = await _context.FailedElasticOperations
                .Where(o => o.Status == "Pending" && o.RetryCount < 5)
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var data = new Dictionary<string, object>
            {
                { "pending_operations", pendingCount },
                { "permanently_failed_operations", failedCount },
                { "oldest_pending_age_minutes", oldestPending != default
                    ? (DateTime.UtcNow - oldestPending).TotalMinutes
                    : 0 }
            };

            if (pendingCount >= criticalThreshold)
            {
                return HealthCheckResult.Unhealthy(
                    $"Dead Letter Queue has {pendingCount} pending operations (critical threshold: {criticalThreshold})",
                    data: data);
            }

            if (pendingCount >= warningThreshold)
            {
                return HealthCheckResult.Degraded(
                    $"Dead Letter Queue has {pendingCount} pending operations (warning threshold: {warningThreshold})",
                    data: data);
            }

            if (failedCount > 0)
            {
                _logger.LogWarning("Dead Letter Queue has {Count} permanently failed operations", failedCount);
            }

            return HealthCheckResult.Healthy(
                "Dead Letter Queue is healthy",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while checking Dead Letter Queue health");
            return HealthCheckResult.Unhealthy(
                "Exception occurred while checking Dead Letter Queue",
                exception: ex);
        }
    }
}