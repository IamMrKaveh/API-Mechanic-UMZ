namespace Infrastructure.Search.Configuration;

/// <summary>
/// Elasticsearch configuration options
/// </summary>
public class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    /// <summary>
    /// Elasticsearch server URL
    /// </summary>
    public string Url { get; set; } = "http://localhost:9200";

    /// <summary>
    /// Default index name
    /// </summary>
    public string Index { get; set; } = "products_v1";

    /// <summary>
    /// Username for authentication (optional)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password for authentication (optional)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// API Key for authentication (optional)
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Enable SSL certificate validation
    /// </summary>
    public bool ValidateCertificate { get; set; } = true;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable debug mode (logs all requests and responses)
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Number of shards for indices
    /// </summary>
    public int NumberOfShards { get; set; } = 1;

    /// <summary>
    /// Number of replicas for indices
    /// </summary>
    public int NumberOfReplicas { get; set; } = 1;

    /// <summary>
    /// Maximum result window size
    /// </summary>
    public int MaxResultWindow { get; set; } = 10000;

    /// <summary>
    /// Enable automatic index creation on startup
    /// </summary>
    public bool AutoCreateIndices { get; set; } = true;

    /// <summary>
    /// Enable background synchronization service
    /// </summary>
    public bool EnableBackgroundSync { get; set; } = true;

    /// <summary>
    /// Background sync interval in minutes
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Batch size for bulk operations
    /// </summary>
    public int BulkBatchSize { get; set; } = 1000;
}

/// <summary>
/// Index configuration for different types
/// </summary>
public class IndexConfiguration
{
    public string Name { get; set; } = default!;
    public int Shards { get; set; } = 1;
    public int Replicas { get; set; } = 1;
    public int MaxResultWindow { get; set; } = 10000;
    public Dictionary<string, AnalyzerConfiguration> Analyzers { get; set; } = new();
}

/// <summary>
/// Analyzer configuration
/// </summary>
public class AnalyzerConfiguration
{
    public string Type { get; set; } = default!;
    public string? Tokenizer { get; set; }
    public List<string> Filters { get; set; } = new();
}

/// <summary>
/// Search configuration and tuning parameters
/// </summary>
public class SearchConfiguration
{
    /// <summary>
    /// Default page size for search results
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// Maximum page size allowed
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// Enable fuzzy matching
    /// </summary>
    public bool EnableFuzzySearch { get; set; } = true;

    /// <summary>
    /// Minimum score threshold for search results
    /// </summary>
    public double? MinScore { get; set; }

    /// <summary>
    /// Enable highlighting in search results
    /// </summary>
    public bool EnableHighlighting { get; set; } = true;

    /// <summary>
    /// Enable suggestions
    /// </summary>
    public bool EnableSuggestions { get; set; } = true;

    /// <summary>
    /// Number of suggestions to return
    /// </summary>
    public int SuggestionCount { get; set; } = 5;

    /// <summary>
    /// Field boost configuration
    /// </summary>
    public Dictionary<string, double> FieldBoosts { get; set; } = new()
    {
        { "name", 5.0 },
        { "categoryName", 3.0 },
        { "brandName", 2.0 },
        { "description", 1.0 }
    };
}

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