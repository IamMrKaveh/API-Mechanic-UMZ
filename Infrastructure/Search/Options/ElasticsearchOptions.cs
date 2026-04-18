namespace Infrastructure.Search.Options;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public bool IsEnabled { get; init; }
    public string Url { get; init; } = "http://localhost:9200";
    public string DefaultIndex { get; init; } = "products";
    public bool EnableBackgroundSync { get; init; }
    public int MaxRetryCount { get; init; } = 3;
    public int RequestTimeoutSeconds { get; init; } = 30;
    public string[] Urls { get; init; } = [];
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool DebugMode { get; init; }
}