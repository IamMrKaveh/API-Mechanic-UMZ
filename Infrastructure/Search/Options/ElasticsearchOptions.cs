namespace Infrastructure.Search.Options;

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string[] Urls { get; init; } = ["http://localhost:9200"];
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public int MaxRetries { get; init; } = 3;
    public bool DebugMode { get; init; }
    public static bool IsEnabled { get; } = true;
}