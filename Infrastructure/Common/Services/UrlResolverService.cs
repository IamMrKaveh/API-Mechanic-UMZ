namespace Infrastructure.Common.Services;

public sealed class UrlResolverService(IConfiguration configuration) : IUrlResolverService
{
    private readonly string _baseUrl = configuration["Storage:BaseUrl"]
        ?? configuration["Liara:BaseUrl"]
        ?? string.Empty;

    public string? ResolveUrl(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return relativePath;

        return $"{_baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }

    public string GetAbsoluteUrl(string relativePath)
    {
        return ResolveUrl(relativePath) ?? relativePath;
    }

    public string ResolveMediaUrl(string filePath)
    {
        throw new NotImplementedException();
    }

    public string ResolveAbsoluteUrl(string relativePath)
    {
        throw new NotImplementedException();
    }
}