namespace Infrastructure.Search.Configurations;

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