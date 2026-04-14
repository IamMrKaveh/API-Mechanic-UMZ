using Amazon.S3;
using Amazon.S3.Transfer;
using Application.Storage.Contracts;
using Infrastructure.Storage.Options;
using MassTransit.Configuration;

namespace Infrastructure.Storage.Services;

public sealed class S3FileStorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> options,
    ILogger<S3FileStorageService> logger) : IFileStorageService
{
    private readonly S3Options _options = options.Value;

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken ct = default)
    {
        var key = BuildKey(fileName, folder);

        var utility = new TransferUtility(s3Client);
        var request = new TransferUtilityUploadRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await utility.UploadAsync(request, ct);

        logger.LogInformation("File uploaded to S3: {Key}", key);
        return BuildPublicUrl(key);
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken ct = default)
    {
        var key = ExtractKeyFromUrl(fileUrl);
        await s3Client.DeleteObjectAsync(_options.BucketName, key, ct);
        logger.LogInformation("File deleted from S3: {Key}", key);
    }

    public string GetPublicUrl(string key) => BuildPublicUrl(key);

    private string BuildKey(string fileName, string? folder)
    {
        var sanitized = fileName.Replace(" ", "_");
        return string.IsNullOrEmpty(folder)
            ? sanitized
            : $"{folder.TrimEnd('/')}/{sanitized}";
    }

    private string BuildPublicUrl(string key)
        => $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{key}";

    private string ExtractKeyFromUrl(string url)
    {
        var baseUrl = $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/";
        return url.Replace(baseUrl, string.Empty);
    }
}