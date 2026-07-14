using Amazon.S3.Model;
using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Infrastructure.Storage.Options;

namespace Infrastructure.BackgroundJobs.Services;

public sealed class S3AuditArchiveStorage(
    IAmazonS3 s3Client,
    IOptions<S3Options> options) : IAuditArchiveStorage
{
    private readonly S3Options _options = options.Value;

    public async Task ArchiveAsync(
        IEnumerable<AuditLog> logs,
        string label,
        DateTime timestamp,
        CancellationToken ct)
    {
        var materialized = logs as IReadOnlyCollection<AuditLog> ?? logs.ToList();
        if (materialized.Count == 0) return;

        var safeLabel = string.IsNullOrWhiteSpace(label) ? "batch" : label;

        var key = $"audit-archives/{timestamp.Year:D4}/{timestamp:yyyy-MM-dd}/{safeLabel}_{timestamp:yyyy-MM-dd_HH-mm}_{Guid.NewGuid():N}.json";
        var json = JsonSerializer.Serialize(materialized);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = "application/json"
        };

        await s3Client.PutObjectAsync(request, ct);
    }
}