namespace Infrastructure.Common.Services;

public sealed class UrlResolverService(IConfiguration configuration) : IUrlResolverService
{
    private readonly string _baseUrl = configuration["Storage:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

    public string? ResolveUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        return $"{_baseUrl}/{relativePath.TrimStart('/')}";
    }
}