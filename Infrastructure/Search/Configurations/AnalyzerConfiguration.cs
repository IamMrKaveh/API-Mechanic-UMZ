namespace Infrastructure.Search.Configurations;

/// <summary>
/// Analyzer configuration
/// </summary>
public class AnalyzerConfiguration
{
    public string Type { get; set; } = default!;
    public string? Tokenizer { get; set; }
    public List<string> Filters { get; set; } = new();
}