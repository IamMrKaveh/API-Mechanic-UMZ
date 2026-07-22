using Amazon.S3.Model;
using Infrastructure.Storage.Options;
using Microsoft.FeatureManagement;
using SharedContracts.FeatureManagement;

namespace Infrastructure.Storage.Services;

public sealed class S3FileStorageService(
    IAmazonS3 s3Client,
    IOptions<StorageOptions> options,
    IAuditService auditService,
    IFeatureManager featureManager) : IStorageService
{
    private readonly StorageOptions _options = options.Value;

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(folder)
            ? $"{Guid.NewGuid()}/{fileName}"
            : $"{folder.Trim('/')}/{Guid.NewGuid()}/{fileName}";

        await using var buffer = await BufferAsync(fileStream, ct);

        var presignedEnabled = await featureManager.IsEnabledAsync(FeatureFlags.StoragePresignedUrlEnabled);

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = buffer,
            ContentType = contentType,
            AutoCloseStream = false,
            UseChunkEncoding = false,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
            CannedACL = presignedEnabled ? S3CannedACL.Private : S3CannedACL.PublicRead
        };

        try
        {
            await s3Client.PutObjectAsync(putRequest, ct);
        }
        catch (AmazonS3Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Storage upload failed for '{fileName}' (key='{key}', status={(int)ex.StatusCode}, code='{ex.ErrorCode}'): {ex.Message}",
                ct);
            throw;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"Storage upload failed for '{fileName}' (key='{key}'): {ex.Message}",
                ct);
            throw;
        }

        await auditService.LogSystemEventAsync(
            "FileUploaded",
            $"فایل '{fileName}' در فضای ذخیره‌سازی '{_options.Provider}' آپلود شد.",
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
            await auditService.LogErrorAsync($"Storage delete failed for '{filePath}': {ex.Message}", ct);
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
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
        return $"{_options.BaseUrl.TrimEnd('/')}/{filePath.TrimStart('/')}";
    }

    public Task<string> GetPresignedUrlAsync(string filePath, TimeSpan expiry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.FromResult(string.Empty);

        var effectiveExpiry = expiry <= TimeSpan.Zero ? TimeSpan.FromMinutes(15) : expiry;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = filePath.TrimStart('/'),
            Expires = DateTime.UtcNow.Add(effectiveExpiry),
            Verb = HttpVerb.GET
        };

        var url = s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    private static async Task<MemoryStream> BufferAsync(Stream source, CancellationToken ct)
    {
        if (source is MemoryStream existing && existing.CanSeek)
        {
            existing.Position = 0;
            return existing;
        }

        var buffer = new MemoryStream();
        if (source.CanSeek)
        {
            source.Position = 0;
        }
        await source.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return buffer;
    }
}
