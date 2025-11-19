namespace Infrastructure.Storage;

public class LiaraStorageService : IStorageService
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucketName;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiEndpoint;
    private readonly ILogger<LiaraStorageService> _logger;

    public LiaraStorageService(IOptions<LiaraStorageSettings> options, ILogger<LiaraStorageService> logger)
    {
        var settings = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(settings.AccessKey) ||
            string.IsNullOrWhiteSpace(settings.SecretKey) ||
            string.IsNullOrWhiteSpace(settings.BucketName) ||
            string.IsNullOrWhiteSpace(settings.BaseUrl) ||
            string.IsNullOrWhiteSpace(settings.ApiEndpoint))
        {
            throw new InvalidOperationException("Liara Storage settings are not fully configured. Please check your configuration.");
        }

        _accessKey = settings.AccessKey;
        _secretKey = settings.SecretKey;
        _bucketName = settings.BucketName;
        _baseUrl = settings.BaseUrl;
        _apiEndpoint = settings.ApiEndpoint;

        _httpClient = new HttpClient { BaseAddress = new Uri(_apiEndpoint) };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessKey}:{_secretKey}");
    }

    private async Task EnsureFolderExistsAsync(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var folderKey = folderPath.TrimEnd('/') + "/";
        var checkUri = $"storage/buckets/{_bucketName}/objects?prefix={folderKey}&max-keys=1";

        try
        {
            var checkResponse = await _httpClient.GetAsync(checkUri);
            checkResponse.EnsureSuccessStatusCode();
            var content = await checkResponse.Content.ReadAsStringAsync();

            var jsonDoc = JsonDocument.Parse(content);
            if (!jsonDoc.RootElement.TryGetProperty("objects", out var objects) || objects.GetArrayLength() == 0)
            {
                _logger.LogInformation("Folder '{FolderPath}' does not exist. Creating it.", folderKey);

                var createFolderContent = new StringContent(string.Empty);
                var createFolderUri = $"storage/buckets/{_bucketName}/objects?key={folderKey}";
                var createResponse = await _httpClient.PostAsync(createFolderUri, createFolderContent);
                createResponse.EnsureSuccessStatusCode();

                _logger.LogInformation("Successfully created folder '{FolderPath}'.", folderKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure folder '{FolderPath}' exists in Liara storage.", folderKey);
            throw;
        }
    }

    public async Task<(string FilePath, string FileName)> SaveFileAsync(Stream stream, string fileName, string entityType, string entityId)
    {
        string folder = $"uploads/{entityType}/{entityId}";
        await EnsureFolderExistsAsync(folder);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var relativePath = $"{folder}/{uniqueFileName}".Replace("//", "/");

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);

        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(streamContent, "file", uniqueFileName);

        var objectKey = relativePath.TrimStart('/');
        var requestUri = $"storage/buckets/{_bucketName}/objects?key={objectKey}";

        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        return (relativePath, uniqueFileName);
    }


    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var path = relativePath.TrimStart('/');
        var response = await _httpClient.DeleteAsync($"storage/buckets/{_bucketName}/objects?key={path}");
        response.EnsureSuccessStatusCode();
    }

    public string GetUrl(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return string.Empty;
        var trimmedPath = relativePath.TrimStart('/');
        return $"{_baseUrl.TrimEnd('/')}/{trimmedPath}";
    }
}