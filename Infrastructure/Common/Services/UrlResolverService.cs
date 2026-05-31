namespace Infrastructure.Common.Services;

public sealed class UrlResolverService(IConfiguration configuration) : IUrlResolverService
{
    private readonly string _baseUrl = configuration["Storage:BaseUrl"]
        ?? configuration["Liara:BaseUrl"]
        ?? string.Empty;

    public string ResolveMediaUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        if (filePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return filePath;

        return $"{_baseUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
    }
}