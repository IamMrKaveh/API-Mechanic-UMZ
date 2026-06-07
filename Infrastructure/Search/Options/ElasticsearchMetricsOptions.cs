namespace Infrastructure.Search.Options;

public class ElasticsearchMetricsOptions
{
    public bool EnableMetrics { get; set; } = true;

    public bool EnableSlowQueryLog { get; set; } = true;

    public int SlowQueryThresholdMs { get; set; } = 1000;

    public bool EnableRequestLogging { get; set; } = false;
}