using System.IO.Compression;
using System.Text.Json;
using Domain.Audit.Entities;
using Domain.User.ValueObjects;
using Infrastructure.Audit.Storage;
using Microsoft.Extensions.Configuration;

namespace Tests.Infrastructure.Audit.Storage;

public class FileSystemAuditArchiveStorageTests : IDisposable
{
    private readonly string _root;
    private readonly ILogger<FileSystemAuditArchiveStorage> _logger = Substitute.For<ILogger<FileSystemAuditArchiveStorage>>();
    private readonly FileSystemAuditArchiveStorage _sut;

    public FileSystemAuditArchiveStorageTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "audit-archive-tests", Guid.NewGuid().ToString("N"));
        var configuration = Substitute.For<IConfiguration>();
        configuration["Audit:ArchivePath"].Returns(_root);
        _sut = new FileSystemAuditArchiveStorage(configuration, _logger);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static AuditLog NewLog(string action = "OrderCreated") =>
        AuditLog.Create(UserId.NewId(), "OrderEvent", action, "127.0.0.1", "Order", Guid.NewGuid().ToString(), "details", "agent");

    private static async Task<string> ReadArchivedJsonAsync(string filePath)
    {
        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task ArchiveAsync_WithEmptyLogs_CreatesNoFile()
    {
        await _sut.ArchiveAsync([], "batch", new DateTime(2026, 3, 1, 10, 30, 0, DateTimeKind.Utc), CancellationToken.None);

        Directory.GetFiles(_root, "*", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public async Task ArchiveAsync_WritesGzippedJsonUnderYearAndDateFolders()
    {
        var logs = new[] { NewLog("OrderCreated"), NewLog("OrderPaid") };
        var timestamp = new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc);

        await _sut.ArchiveAsync(logs, "default", timestamp, CancellationToken.None);

        var files = Directory.GetFiles(_root, "*.json.gz", SearchOption.AllDirectories);
        files.Length.ShouldBe(1);
        files[0].ShouldContain(Path.Combine("2026", "2026-03-15"));
        Path.GetFileName(files[0]).ShouldStartWith("default_2026-03-15_10-30_");
    }

    [Fact]
    public async Task ArchiveAsync_RoundTripsLogContents()
    {
        var logs = new[] { NewLog("RefundIssued") };

        await _sut.ArchiveAsync(logs, "financial", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        var file = Directory.GetFiles(_root, "*.json.gz", SearchOption.AllDirectories).Single();
        var json = await ReadArchivedJsonAsync(file);
        var documents = JsonSerializer.Deserialize<List<JsonElement>>(json);

        documents.ShouldNotBeNull();
        documents!.Count.ShouldBe(1);
        documents[0].GetProperty("Action").GetString().ShouldBe("RefundIssued");
        documents[0].GetProperty("EntityType").GetString().ShouldBe("Order");
    }

    [Fact]
    public async Task ArchiveAsync_WithBlankLabel_FallsBackToBatch()
    {
        await _sut.ArchiveAsync([NewLog()], "   ", new DateTime(2026, 1, 5, 1, 2, 0, DateTimeKind.Utc), CancellationToken.None);

        var file = Directory.GetFiles(_root, "*.json.gz", SearchOption.AllDirectories).Single();
        Path.GetFileName(file).ShouldStartWith("batch_2026-01-05_01-02_");
    }

    [Fact]
    public async Task ArchiveAsync_CalledTwice_CreatesSeparateFiles()
    {
        var timestamp = new DateTime(2026, 2, 2, 2, 2, 0, DateTimeKind.Utc);

        await _sut.ArchiveAsync([NewLog("A")], "default", timestamp, CancellationToken.None);
        await _sut.ArchiveAsync([NewLog("B")], "default", timestamp, CancellationToken.None);

        Directory.GetFiles(_root, "*.json.gz", SearchOption.AllDirectories).Length.ShouldBe(2);
    }

    [Fact]
    public void Constructor_WithoutConfiguredPath_CreatesDefaultDirectory()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Audit:ArchivePath"].Returns((string?)null);

        Should.NotThrow(() => new FileSystemAuditArchiveStorage(configuration, _logger));
    }
}
