using Amazon.S3;
using Amazon.S3.Transfer;
using Application.Audit.Contracts;
using Application.Media.Contracts;
using Infrastructure.Storage.Options;

namespace Infrastructure.Storage.Services;

public sealed class S3FileStorageService(
    IAmazonS3 s3Client,
    IOptions<S3Options> options,
    IAuditService auditService) : IStorageService
{
    private readonly S3Options _options = options.Value;

    public async Task<string> UploadAsync(
        Stream fileStream, string fileName, string contentType,
        string? folder = null, CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(folder)
            ? $"{Guid.NewGuid()}/{fileName}"
            : $"{folder.Trim('/')}/{Guid.NewGuid()}/{fileName}";

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = fileStream,
            Key = key,
            BucketName = _options.BucketName,
            ContentType = contentType
        };

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(uploadRequest, ct);

        await auditService.LogSystemEventAsync(
            "FileUploaded",
            $"فایل '{fileName}' در S3 آپلود شد.",
            ct);

        return key;
    }

    public async Task<bool> DeleteAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            await s3Client.DeleteObjectAsync(_options.BucketName, filePath, ct);
            return true;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"S3 delete failed for '{filePath}': {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            await s3Client.GetObjectMetadataAsync(_options.BucketName, filePath, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public string GetPublicUrl(string filePath)
        => $"https://{_options.BucketName}.s3.{_options.Region}.amazonaws.com/{filePath.TrimStart('/')}";
}