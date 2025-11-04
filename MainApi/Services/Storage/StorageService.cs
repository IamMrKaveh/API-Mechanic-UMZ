namespace MainApi.Services.Storage;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder, int? entityId = null);
    Task DeleteFileAsync(string relativePath);
}

public class LiaraStorageService : IStorageService
{
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _bucketName;
    private readonly HttpClient _httpClient;

    public LiaraStorageService(IConfiguration configuration)
    {
        _accessKey = configuration["LiaraStorage:AccessKey"];
        _secretKey = configuration["LiaraStorage:SecretKey"];
        _bucketName = configuration["LiaraStorage:BucketName"];
        _httpClient = new HttpClient { BaseAddress = new Uri("https://api.liara.ir/") };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessKey}:{_secretKey}");
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder, int? entityId = null)
    {
        var extension = Path.GetExtension(file.FileName).ToLower();
        var fileName = entityId.HasValue
            ? $"{entityId.Value}{extension}"
            : $"{Guid.NewGuid()}{extension}";

        var relativePath = $"/{folder}/{fileName}".Replace("//", "/");

        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", fileName);

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
}