namespace MainApi.Services.Storage;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder);
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

    public async Task<string> UploadFileAsync(IFormFile file, string folder)
    {
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var relativePath = $"{folder}/{fileName}";
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", fileName);
        var response = await _httpClient.PostAsync($"/storage/buckets/{_bucketName}/upload/{relativePath}", content);
        response.EnsureSuccessStatusCode();
        return "/" + relativePath;
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var path = relativePath.TrimStart('/');
        var response = await _httpClient.DeleteAsync($"/storage/buckets/{_bucketName}/files/{path}");
        response.EnsureSuccessStatusCode();
    }
}
