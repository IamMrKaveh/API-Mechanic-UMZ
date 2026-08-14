using Domain.Audit.Entities;
using Infrastructure.BackgroundJobs.Services;
using Infrastructure.Storage.Options;

namespace Tests.Infrastructure.BackgroundJobs.Services;

public class S3AuditArchiveStorageTests
{
    private static S3AuditArchiveStorage CreateSut(IAmazonS3 s3Client, string bucketName = "audit-bucket")
    { var options = Microsoft.Extensions.Options.Options.Create(new S3Options { BucketName = bucketName }); return new S3AuditArchiveStorage(s3Client, options); }

    private static AuditLog CreateAuditLog(string eventType = "TestEvent", string action = "TestAction")
    {
        return AuditLog.Create(
            userId: null,
            eventType: eventType,
            action: action,
            ipAddress: "127.0.0.1");
    }

    private sealed class CapturedRequest
    {
        public string? Bucket { get; set; }
        public string? Key { get; set; }
        public string? ContentType { get; set; }
        public string? Body { get; set; }
    }

    private static CapturedRequest ConfigureCapture(IAmazonS3 s3Client)
    {
        var captured = new CapturedRequest();
        s3Client
            .PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var request = ci.Arg<PutObjectRequest>();
                captured.Bucket = request!.BucketName;
                captured.Key = request.Key;
                captured.ContentType = request.ContentType;
                if (request.InputStream is not null)
                {
                    var position = request.InputStream.CanSeek ? request.InputStream.Position : 0L;
                    if (request.InputStream.CanSeek)
                        request.InputStream.Position = 0;
                    using var reader = new StreamReader(request.InputStream, leaveOpen: true);
                    captured.Body = reader.ReadToEnd();
                    if (request.InputStream.CanSeek)
                        request.InputStream.Position = position;
                }
                return Task.FromResult(new PutObjectResponse());
            });
        return captured;
    }

    [Fact]
    public async Task ArchiveAsync_WithEmptyLogs_DoesNotCallS3()
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var sut = CreateSut(s3Client);

        await sut.ArchiveAsync(
            System.Linq.Enumerable.Empty<AuditLog>(),
            label: "default",
            timestamp: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            ct: CancellationToken.None);

        await s3Client.DidNotReceiveWithAnyArgs()
            .PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ArchiveAsync_WithLogs_UploadsToConfiguredBucket()
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var captured = ConfigureCapture(s3Client);
        var sut = CreateSut(s3Client, bucketName: "custom-audit-bucket");
        var logs = new List<AuditLog> { CreateAuditLog() };

        await sut.ArchiveAsync(
            logs,
            label: "default",
            timestamp: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            ct: CancellationToken.None);

        await s3Client.Received(1).PutObjectAsync(
            Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>());
        captured.Bucket.ShouldBe("custom-audit-bucket");
    }

    [Fact]
    public async Task ArchiveAsync_WithLogs_UsesJsonContentType()
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var captured = ConfigureCapture(s3Client);
        var sut = CreateSut(s3Client);
        var logs = new List<AuditLog> { CreateAuditLog() };

        await sut.ArchiveAsync(
            logs,
            label: "default",
            timestamp: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            ct: CancellationToken.None);

        captured.ContentType.ShouldBe("application/json");
    }

    [Fact]
    public async Task ArchiveAsync_WithLogs_KeyFollowsExpectedPathPattern()
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var captured = ConfigureCapture(s3Client);
        var sut = CreateSut(s3Client);
        var logs = new List<AuditLog> { CreateAuditLog() };
        var timestamp = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);

        await sut.ArchiveAsync(logs, label: "financial", timestamp: timestamp, ct: CancellationToken.None);

        captured.Key.ShouldNotBeNull();
        captured.Key!.ShouldStartWith("audit-archives/2024/2024-03-15/financial_2024-03-15_10-30_");
        captured.Key.ShouldEndWith(".json");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ArchiveAsync_WithNullOrWhitespaceLabel_UsesBatchSegment(string? label)
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var captured = ConfigureCapture(s3Client);
        var sut = CreateSut(s3Client);
        var logs = new List<AuditLog> { CreateAuditLog() };
        var timestamp = new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc);

        await sut.ArchiveAsync(logs, label: label!, timestamp: timestamp, ct: CancellationToken.None);

        captured.Key.ShouldNotBeNull();
        captured.Key!.ShouldStartWith("audit-archives/2024/2024-03-15/batch_2024-03-15_10-30_");
        captured.Key.ShouldEndWith(".json");
    }

    [Fact]
    public async Task ArchiveAsync_WithLogs_WritesNonEmptyBody()
    {
        var s3Client = Substitute.For<IAmazonS3>();
        var captured = ConfigureCapture(s3Client);
        var sut = CreateSut(s3Client);
        var logs = new List<AuditLog>
    {
        CreateAuditLog(eventType: "PaymentEvent", action: "PaymentSettled"),
        CreateAuditLog(eventType: "OrderEvent", action: "OrderCreated")
    };

        await sut.ArchiveAsync(
            logs,
            label: "financial",
            timestamp: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            ct: CancellationToken.None);

        captured.Body.ShouldNotBeNull();
        captured.Body!.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ArchiveAsync_ForwardsCancellationTokenToS3Client()
    {
        var s3Client = Substitute.For<IAmazonS3>();
        ConfigureCapture(s3Client);
        var sut = CreateSut(s3Client);
        var logs = new List<AuditLog> { CreateAuditLog() };
        using var cts = new CancellationTokenSource();

        await sut.ArchiveAsync(
            logs,
            label: "default",
            timestamp: new DateTime(2024, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            ct: cts.Token);

        await s3Client.Received(1).PutObjectAsync(
            Arg.Any<PutObjectRequest>(),
            Arg.Is<CancellationToken>(t => t == cts.Token));
    }
}
