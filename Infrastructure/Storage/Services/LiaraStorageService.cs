using Amazon.S3.Model;
using Infrastructure.Storage.Options;

namespace Infrastructure.Storage.Services;

public sealed class LiaraStorageService(
    IAmazonS3 s3Client,
    IOptions<LiaraStorageOptions> options,
    IAuditService auditService) : IStorageService
{
    private readonly LiaraStorageOptions _options = options.Value;

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType,
        string? directory = null,
        CancellationToken ct = default)
    {
        var key = string.IsNullOrWhiteSpace(directory)
            ? $"{Guid.NewGuid()}/{fileName}"
            : $"{directory.Trim('/')}/{Guid.NewGuid()}/{fileName}";

        await using var buffer = await BufferAsync(stream, ct);

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
            CannedACL = S3CannedACL.PublicRead
        };

        try
        {
            await s3Client.PutObjectAsync(putRequest, ct);
        }
        catch (AmazonS3Exception ex)
        {
            await auditService.LogErrorAsync(
                $"S3 upload failed for '{fileName}' (key='{key}', status={(int)ex.StatusCode}, code='{ex.ErrorCode}'): {ex.Message}",
                ct);
            throw;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"S3 upload failed for '{fileName}' (key='{key}'): {ex.Message}",
                ct);
            throw;
        }

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