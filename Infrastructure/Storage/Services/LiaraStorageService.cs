namespace Infrastructure.Storage.Services;

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

    public async Task DeleteFileAsync(
        string relativePath,
        CancellationToken ct)
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

            await _s3Client.DeleteObjectAsync(deleteRequest, ct);
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

        if (relativePath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return relativePath;

        var trimmedPath = relativePath.TrimStart('/');
        return $"{_baseUrl.TrimEnd('/')}/{trimmedPath}";
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string prefix, int maxKeys = 1000, string? continuationToken = null)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = prefix,
                MaxKeys = maxKeys,
                ContinuationToken = continuationToken
            };

            var response = await _s3Client.ListObjectsV2Async(request);
            return response.S3Objects.Select(o => o.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from storage");
            return Enumerable.Empty<string>();
        }
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
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    public async Task<Stream?> GetFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var key = filePath.TrimStart('/');
            var response = await _s3Client.GetObjectAsync(_bucketName, key, ct);
            return response.ResponseStream;
        }
        catch (Amazon.S3.AmazonS3Exception)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {Path}", filePath);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            var key = filePath.TrimStart('/');
            await _s3Client.GetObjectMetadataAsync(_bucketName, key, ct);
            return true;
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file existence {Path}", filePath);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetFilesAsync(string directory, int maxResults, string? continuationToken, CancellationToken ct = default)
    {
        try
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName,
                Prefix = directory,
                MaxKeys = maxResults,
                ContinuationToken = continuationToken
            };

            var response = await _s3Client.ListObjectsV2Async(request, ct);
            return response.S3Objects.Select(o => o.Key).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from storage");
            return new List<string>();
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string directory, CancellationToken ct = default)
    {
        var (path, _) = await SaveFileAsync(fileStream, fileName, directory.Trim('/'), "");
        return path;
    }
}