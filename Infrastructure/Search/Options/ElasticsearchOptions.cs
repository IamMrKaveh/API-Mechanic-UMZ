namespace Infrastructure.Search.Options;

/// <summary>
/// Elasticsearch configuration options
/// </summary>
public sealed class ElasticsearchOptions
{
    public const bool IsEnabled = false;
    public const string SectionName = "Elasticsearch";
    public string Url { get; init; } = string.Empty;
    public ElasticsearchIndexOptions Indexes { get; init; } = new();
}