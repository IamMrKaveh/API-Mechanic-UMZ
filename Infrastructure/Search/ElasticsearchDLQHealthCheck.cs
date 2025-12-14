namespace Infrastructure.Search;

public class ElasticsearchDLQHealthCheck : IHealthCheck
{
    private readonly LedkaContext _context;
    private readonly ILogger<ElasticsearchDLQHealthCheck> _logger;
    private readonly IConfiguration _configuration;

    public ElasticsearchDLQHealthCheck(
        LedkaContext context,
        ILogger<ElasticsearchDLQHealthCheck> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var warningThreshold = _configuration.GetValue<int>("Elasticsearch:DLQWarningThreshold", 100);
            var criticalThreshold = _configuration.GetValue<int>("Elasticsearch:DLQCriticalThreshold", 500);

            var pendingCount = await _context.FailedElasticOperations
                .CountAsync(o => o.Status == "Pending" && o.RetryCount < 5, cancellationToken);

            var failedCount = await _context.FailedElasticOperations
                .CountAsync(o => o.RetryCount >= 5, cancellationToken);

            var oldestPending = await _context.FailedElasticOperations
                .Where(o => o.Status == "Pending" && o.RetryCount < 5)
                .OrderBy(o => o.CreatedAt)
                .Select(o => o.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

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