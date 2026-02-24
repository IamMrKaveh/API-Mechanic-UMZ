namespace Infrastructure.Common.Services;

public sealed class UrlResolverService : IUrlResolverService
{
    private readonly string _baseUrl;

    public UrlResolverService(IConfiguration configuration)
    {
        _baseUrl = configuration["Storage:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
    }

    public string? ResolveUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        return $"{_baseUrl}/{relativePath.TrimStart('/')}";
    }
}