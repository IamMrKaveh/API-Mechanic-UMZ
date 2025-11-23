namespace Infrastructure.Storage;

public class LiaraStorageService : IStorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly string _bucketName;
    private readonly string _baseUrl;
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

        _bucketName = settings.BucketName;
        _baseUrl = settings.BaseUrl;

        var config = new AmazonS3Config
        {
            ServiceURL = settings.ApiEndpoint,
            ForcePathStyle = true,
        };

        var credentials = new BasicAWSCredentials(settings.AccessKey, settings.SecretKey);
        _s3Client = new AmazonS3Client(credentials, config);
    }

    public async Task<(string FilePath, string FileName)> SaveFileAsync(
        Stream stream,
        string fileName,
        string entityType,
        string entityId)
    {
        try
        {
            var folder = $"uploads/{entityType}/{entityId}/";

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var key = $"{folder}{uniqueFileName}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                AutoCloseStream = false,
                ContentType = GetContentType(extension)
            };

            await _s3Client.PutObjectAsync(putRequest);

            return (key, uniqueFileName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "S3 Error uploading file {FileName}", fileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", fileName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return;

        try
        {
            var key = relativePath.TrimStart('/');

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Path}", relativePath);
            throw;
        }
    }

    public string GetUrl(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return string.Empty;

        var trimmedPath = relativePath.TrimStart('/');
        return $"{_baseUrl.TrimEnd('/')}/{trimmedPath}";
    }

    private string GetContentType(string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}