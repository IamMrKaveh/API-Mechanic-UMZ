namespace Infrastructure.Chaos.Options;

public sealed class ChaosOptions
{
    public const string SectionName = "Chaos";

    public bool IsEnabled { get; init; } = false;

    public double LatencyInjectionRate { get; init; } = 0.0;

    public int MaxLatencyMilliseconds { get; init; } = 5000;

    public double FaultInjectionRate { get; init; } = 0.0;

    public string[] IncludedPathPrefixes { get; init; } = [];

    public string[] ExcludedPathPrefixes { get; init; } =
    [
        "/health",
        "/metrics",
        "/swagger"
    ];
}
