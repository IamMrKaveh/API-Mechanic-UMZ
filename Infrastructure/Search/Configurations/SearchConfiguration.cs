namespace Infrastructure.Search.Configurations;

public class SearchConfiguration
{
    public int DefaultPageSize { get; set; } = 20;

    public int MaxPageSize { get; set; } = 100;

    public bool EnableFuzzySearch { get; set; } = true;

    public double? MinScore { get; set; }

    public bool EnableHighlighting { get; set; } = true;

    public bool EnableSuggestions { get; set; } = true;

    public int SuggestionCount { get; set; } = 5;

    public Dictionary<string, double> FieldBoosts { get; set; } = new()
    {
        { "name", 5.0 },
        { "categoryName", 3.0 },
        { "brandName", 2.0 },
        { "description", 1.0 }
    };
}