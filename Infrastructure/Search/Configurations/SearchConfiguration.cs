namespace Infrastructure.Search.Configurations;

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