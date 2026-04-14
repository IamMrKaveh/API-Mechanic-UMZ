using Amazon.S3.Transfer;
using Infrastructure.Storage.Options;

namespace Infrastructure.Storage.Services;

public sealed class LiaraStorageService(
    IAmazonS3 s3Client,
    IOptions<LiaraStorageOptions> options,
    IAuditService auditService) : IStorageService
{
    private readonly LiaraStorageOptions _options = options.Value;

    public async Task<string> UploadAsync(
        Stream stream, string fileName, string contentType,
        string? directory = null, CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(directory)
            ? $"{Guid.NewGuid()}/{fileName}"
            : $"{directory.Trim('/')}/{Guid.NewGuid()}/{fileName}";

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            Key = key,
            BucketName = _options.BucketName,
            ContentType = contentType
        };

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(uploadRequest, ct);

        await auditService.LogSystemEventAsync(
            "FileUploaded",
            $"فایل '{fileName}' آپلود شد.",
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
        catch
        {
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
        => $"{_options.BaseUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
}