namespace Infrastructure.Search.Options;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public bool IsEnabled { get; init; }

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string Url { get; init; } = "http://localhost:9200";

    [Required(AllowEmptyStrings = false)]
    public string DefaultIndex { get; init; } = "products";

    public bool EnableBackgroundSync { get; init; }

    [Range(0, 10)]
    public int MaxRetryCount { get; init; } = 3;

    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; init; } = 30;

    public string[] Urls { get; init; } = [];

    [Range(1, 300)]
    public int TimeoutSeconds { get; init; } = 30;

    [Range(0, 10)]
    public int MaxRetries { get; init; } = 3;

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public bool DebugMode { get; init; }
}