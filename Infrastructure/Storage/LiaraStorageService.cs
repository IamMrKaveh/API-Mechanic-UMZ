namespace Infrastructure.Storage;

public class LiaraStorageService : IStorageService
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucketName;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiEndpoint;

    public LiaraStorageService(IOptions<LiaraStorageSettings> options)
    {
        var settings = options.Value;

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

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string folder, int? entityId = null)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        var relativePath = $"{folder}/{uniqueFileName}".Replace("//", "/");

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", uniqueFileName);

        var objectKey = relativePath.TrimStart('/');
        var requestUri = $"storage/buckets/{_bucketName}/objects?key={objectKey}";
        var response = await _httpClient.PostAsync(requestUri, content);
        response.EnsureSuccessStatusCode();

        return relativePath;
    }


    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var path = relativePath.TrimStart('/');
        var response = await _httpClient.DeleteAsync($"storage/buckets/{_bucketName}/objects?key={path}");
        response.EnsureSuccessStatusCode();
    }

    public string GetFileUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return string.Empty;
        var trimmedPath = relativePath.TrimStart('/');
        return $"{_baseUrl.TrimEnd('/')}/{trimmedPath}";
    }
}