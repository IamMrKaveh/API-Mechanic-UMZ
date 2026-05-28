namespace Presentation.Common.Options;

public sealed class ProxySettings
{
    public const string SectionName = "ReverseProxy";
    public List<string> TrustedProxies { get; init; } = [];
    public List<string> TrustedNetworks { get; init; } = [];
}