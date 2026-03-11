namespace Infrastructure.Search.Options;

/// <summary>
/// Performance monitoring and metrics configuration
/// </summary>
public class ElasticsearchMetricsOptions
{
    /// <summary>
    /// Enable performance metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable slow query logging
    /// </summary>
    public bool EnableSlowQueryLog { get; set; } = true;

    /// <summary>
    /// Slow query threshold in milliseconds
    /// </summary>
    public int SlowQueryThresholdMs { get; set; } = 1000;

    /// <summary>
    /// Enable request/response logging
    /// </summary>
    public bool EnableRequestLogging { get; set; } = false;
}