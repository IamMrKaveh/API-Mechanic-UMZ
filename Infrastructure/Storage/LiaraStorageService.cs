namespace Infrastructure.Storage;

public class LiaraStorageService : IStorageService
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucketName;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public LiaraStorageService(IOptions<LiaraStorageSettings> options)
    {
        var settings = options.Value;
        _accessKey = settings.AccessKey!;
        _secretKey = settings.SecretKey!;
        _bucketName = settings.BucketName!;
        _baseUrl = settings.BaseUrl!;
        _httpClient = new HttpClient { BaseAddress = new Uri("https://api.liara.ir/") };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessKey}:{_secretKey}");
    }

    public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, string folder, int? entityId = null)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        var uniqueFileName = entityId.HasValue
            ? $"{entityId.Value}{extension}"
            : $"{Guid.NewGuid()}{extension}";

        var relativePath = $"/{folder}/{uniqueFileName}".Replace("//", "/");

        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", uniqueFileName);

        var objectKey = relativePath.TrimStart('/');
        var response = await _httpClient.PostAsync($"/storage/buckets/{_bucketName}/objects?key={objectKey}", content);
        response.EnsureSuccessStatusCode();

        return relativePath;
    }


    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var path = relativePath.TrimStart('/');
        var response = await _httpClient.DeleteAsync($"/storage/buckets/{_bucketName}/objects/{path}");
        response.EnsureSuccessStatusCode();
    }

    public string GetFileUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return string.Empty;
        return $"{_baseUrl}{relativePath}";
    }
}