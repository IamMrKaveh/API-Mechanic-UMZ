using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using System.IO.Compression;

namespace Infrastructure.Audit.Storage;

public sealed class FileSystemAuditArchiveStorage(
    IConfiguration configuration,
    ILogger<FileSystemAuditArchiveStorage> logger) : IAuditArchiveStorage
{
    private static readonly JsonSerializerOptions ArchiveJsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _archiveRoot = ResolveArchiveRoot(configuration);

    public async Task ArchiveAsync(
        IEnumerable<AuditLog> logs,
        string label,
        DateTime timestamp,
        CancellationToken ct)
    {
        var materialized = logs as IReadOnlyCollection<AuditLog> ?? logs.ToList();
        if (materialized.Count == 0) return;

        var safeLabel = string.IsNullOrWhiteSpace(label) ? "batch" : label;

        var directory = Path.Combine(
            _archiveRoot,
            timestamp.Year.ToString("D4"),
            timestamp.ToString("yyyy-MM-dd"));

        Directory.CreateDirectory(directory);

        var fileName = $"{safeLabel}_{timestamp:yyyy-MM-dd_HH-mm}_{Guid.NewGuid():N}.json.gz";
        var fullPath = Path.Combine(directory, fileName);

        await using (var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        await using (var gzip = new GZipStream(fileStream, CompressionLevel.Optimal))
        {
            await JsonSerializer.SerializeAsync(gzip, materialized, ArchiveJsonOptions, ct);
        }

        logger.LogInformation(
            "Archived {Count} audit logs with label {Label} to {Path}",
            materialized.Count,
            safeLabel,
            fullPath);
    }

    private static string ResolveArchiveRoot(IConfiguration configuration)
    {
        var configured = configuration["Audit:ArchivePath"];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "audit_archives")
            : configured;

        Directory.CreateDirectory(root);
        return root;
    }
}