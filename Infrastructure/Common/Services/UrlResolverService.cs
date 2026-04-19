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
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        if (filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return filePath;

        return $"{_baseUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
    }

    public string ResolveAbsoluteUrl(string relativePath)
    {
        return GetAbsoluteUrl(relativePath);
    }
}