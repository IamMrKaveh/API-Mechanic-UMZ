using Application.Audit.Contracts;
using Application.Storage.Contracts;
using Domain.User.ValueObjects;
using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;
using SharedContracts.FeatureManagement;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;

namespace Tests.Infrastructure.Storage.Services;

public class S3FileStorageServiceTests
{
    private readonly IAmazonS3 _s3 = Substitute.For<IAmazonS3>(); private readonly IAuditService _audit = Substitute.For<IAuditService>(); private readonly IFeatureManager _featureManager = Substitute.For<IFeatureManager>(); private readonly IFileScanningService _scanner = Substitute.For<IFileScanningService>(); private readonly IFileMagicBytesValidator _magicBytes = Substitute.For<IFileMagicBytesValidator>();

    private readonly StorageOptions _defaultOptions = new()
    {
        Provider = "S3",
        BucketName = "mechanic-test",
        BaseUrl = "https://cdn.example.com",
        Endpoint = "https://s3.example.com",
        AccessKey = "ak",
        SecretKey = "sk",
        Region = "us-east-1",
        ForcePathStyle = true,
        UseHttp = false,
        MaxFileSizeBytes = 10_485_760
    };

    private S3FileStorageService CreateSut(StorageOptions? options = null) =>
        new(
            _s3,
            Microsoft.Extensions.Options.Options.Create(options ?? _defaultOptions),
            _audit,
            _featureManager,
            _scanner,
            _magicBytes);

    private void ConfigureAllowedContent(bool magicBytesAllowed = true, bool scanClean = true, bool presignedEnabled = false)
    {
        _magicBytes.IsAllowedAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(magicBytesAllowed);

        _scanner.ScanAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(scanClean
                ? FileScanResult.Clean()
                : FileScanResult.Infected("EICAR-Test-Signature", "stream: EICAR-Test-Signature FOUND"));

        _featureManager.IsEnabledAsync(FeatureFlags.StoragePresignedUrlEnabled).Returns(presignedEnabled);
    }

    [Fact]
    public async Task UploadAsync_WhenMagicBytesRejectDeclaredContentType_ThrowsDomainException()
    {
        ConfigureAllowedContent(magicBytesAllowed: false);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });

        await Should.ThrowAsync<DomainException>(async () =>
            await sut.UploadAsync(stream, "spoofed.png", "image/png"));
    }

    [Fact]
    public async Task UploadAsync_WhenMagicBytesRejectDeclaredContentType_LogsSecurityEventAndSkipsUploadAndScan()
    {
        ConfigureAllowedContent(magicBytesAllowed: false);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });

        await Should.ThrowAsync<DomainException>(async () =>
            await sut.UploadAsync(stream, "spoofed.png", "image/png"));

        await _audit.Received(1).LogSecurityEventAsync(
            "MaliciousUploadDetected",
            Arg.Is<string>(s => s!.Contains("spoofed.png") && s.Contains("magic-byte")),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<CancellationToken>());

        await _scanner.DidNotReceiveWithAnyArgs().ScanAsync(default!, default!, default);
        await _s3.DidNotReceiveWithAnyArgs().PutObjectAsync(default!, default);
    }

    [Fact]
    public async Task UploadAsync_WhenAntivirusReportsInfectedFile_ThrowsDomainException()
    {
        ConfigureAllowedContent(scanClean: false);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        await Should.ThrowAsync<DomainException>(async () =>
            await sut.UploadAsync(stream, "malware.png", "image/png"));
    }

    [Fact]
    public async Task UploadAsync_WhenAntivirusReportsInfectedFile_LogsSecurityEventAndSkipsS3Put()
    {
        ConfigureAllowedContent(scanClean: false);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        await Should.ThrowAsync<DomainException>(async () =>
            await sut.UploadAsync(stream, "malware.png", "image/png"));

        await _audit.Received(1).LogSecurityEventAsync(
            "MaliciousUploadDetected",
            Arg.Is<string>(s =>
                s!.Contains("malware.png") &&
                s.Contains("EICAR-Test-Signature")),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<CancellationToken>());

        await _s3.DidNotReceiveWithAnyArgs().PutObjectAsync(default!, default);
    }

    [Fact]
    public async Task UploadAsync_HappyPathWithoutFolder_ReturnsKeyWithGuidPrefixAndOriginalFileName()
    {
        ConfigureAllowedContent();
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var key = await sut.UploadAsync(stream, "photo.png", "image/png");

        key.ShouldMatch(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/photo\.png$");
    }

    [Fact]
    public async Task UploadAsync_HappyPathWithFolder_PrefixesKeyWithTrimmedFolder()
    {
        ConfigureAllowedContent();
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var key = await sut.UploadAsync(stream, "photo.png", "image/png", "/images/products/");

        key.ShouldMatch(@"^images/products/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}/photo\.png$");
    }

    [Fact]
    public async Task UploadAsync_HappyPathWhenPresignedFeatureDisabled_SendsPutObjectRequestWithPublicReadAcl()
    {
        ConfigureAllowedContent(presignedEnabled: false);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var key = await sut.UploadAsync(stream, "photo.png", "image/png");

        await _s3.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r =>
                r!.BucketName == "mechanic-test" &&
                r.Key == key &&
                r.ContentType == "image/png" &&
                r.CannedACL == S3CannedACL.PublicRead &&
                r.AutoCloseStream == false &&
                r.UseChunkEncoding == false &&
                r.DisablePayloadSigning == true &&
                r.DisableDefaultChecksumValidation == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_HappyPathWhenPresignedFeatureEnabled_SendsPutObjectRequestWithPrivateAcl()
    {
        ConfigureAllowedContent(presignedEnabled: true);
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        var key = await sut.UploadAsync(stream, "photo.png", "image/png");

        await _s3.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r =>
                r!.Key == key &&
                r.CannedACL == S3CannedACL.Private),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_HappyPath_WritesFileUploadedSystemAudit()
    {
        ConfigureAllowedContent();
        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        _ = await sut.UploadAsync(stream, "photo.png", "image/png");

        await _audit.Received(1).LogSystemEventAsync(
            "FileUploaded",
            Arg.Is<string>(s => s!.Contains("photo.png") && s!.Contains(_defaultOptions.Provider)),
            Arg.Any<CancellationToken>());

        await _audit.DidNotReceive().LogSecurityEventAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IpAddress>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenS3ThrowsAmazonS3Exception_LogsErrorAndRethrows()
    {
        ConfigureAllowedContent();
        _s3.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("boom"));

        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        await Should.ThrowAsync<AmazonS3Exception>(async () =>
            await sut.UploadAsync(stream, "photo.png", "image/png"));

        await _audit.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Storage upload failed") && s!.Contains("photo.png")),
            Arg.Any<CancellationToken>());

        await _audit.DidNotReceive().LogSystemEventAsync(
            "FileUploaded", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenS3ThrowsGenericException_LogsErrorAndRethrows()
    {
        ConfigureAllowedContent();
        _s3.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("kaboom"));

        var sut = CreateSut();
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.UploadAsync(stream, "photo.png", "image/png"));

        await _audit.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Storage upload failed") && s!.Contains("photo.png")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenS3Succeeds_ReturnsTrueAndDoesNotLogError()
    {
        var sut = CreateSut();

        var deleted = await sut.DeleteAsync("some/key.png");

        deleted.ShouldBeTrue();
        await _s3.Received(1).DeleteObjectAsync("mechanic-test", "some/key.png", Arg.Any<CancellationToken>());
        await _audit.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task DeleteAsync_WhenS3Throws_ReturnsFalseAndLogsError()
    {
        _s3.DeleteObjectAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("nope"));
        var sut = CreateSut();

        var deleted = await sut.DeleteAsync("some/key.png");

        deleted.ShouldBeFalse();
        await _audit.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Storage delete failed") && s!.Contains("some/key.png")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WhenMetadataFetchSucceeds_ReturnsTrue()
    {
        var sut = CreateSut();

        var exists = await sut.ExistsAsync("some/key.png");

        exists.ShouldBeTrue();
        await _s3.Received(1).GetObjectMetadataAsync("mechanic-test", "some/key.png", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_WhenMetadataFetchThrows_ReturnsFalse()
    {
        _s3.GetObjectMetadataAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonS3Exception("missing"));
        var sut = CreateSut();

        var exists = await sut.ExistsAsync("does/not/exist.png");

        exists.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPublicUrl_WhenPathIsBlank_ReturnsEmptyString(string? path)
    {
        var sut = CreateSut();

        var url = sut.GetPublicUrl(path!);

        url.ShouldBe(string.Empty);
    }

    [Fact]
    public void GetPublicUrl_WithBaseUrlTrailingSlashAndPathLeadingSlash_NormalisesToSingleSlash()
    {
        var options = new StorageOptions
        {
            Provider = _defaultOptions.Provider,
            BucketName = _defaultOptions.BucketName,
            BaseUrl = "https://cdn.example.com/",
            Endpoint = _defaultOptions.Endpoint,
            AccessKey = _defaultOptions.AccessKey,
            SecretKey = _defaultOptions.SecretKey,
            Region = _defaultOptions.Region,
            ForcePathStyle = _defaultOptions.ForcePathStyle,
            UseHttp = _defaultOptions.UseHttp,
            MaxFileSizeBytes = _defaultOptions.MaxFileSizeBytes
        };
        var sut = CreateSut(options);

        var url = sut.GetPublicUrl("/folder/file.png");

        url.ShouldBe("https://cdn.example.com/folder/file.png");
    }

    [Fact]
    public void GetPublicUrl_WithBaseUrlAndPlainPath_ConcatenatesWithSingleSlash()
    {
        var sut = CreateSut();

        var url = sut.GetPublicUrl("folder/file.png");

        url.ShouldBe("https://cdn.example.com/folder/file.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPresignedUrlAsync_WhenPathIsBlank_ReturnsEmptyString(string? path)
    {
        var sut = CreateSut();

        var url = await sut.GetPresignedUrlAsync(path!, TimeSpan.FromMinutes(5));

        url.ShouldBe(string.Empty);
        _s3.DidNotReceive().GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>());
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WhenExpiryIsZeroOrNegative_UsesFifteenMinuteDefault()
    {
        _s3.GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>()).Returns("https://signed.example.com/x");
        var sut = CreateSut();
        var before = DateTime.UtcNow;

        var url = await sut.GetPresignedUrlAsync("folder/file.png", TimeSpan.Zero);

        url.ShouldBe("https://signed.example.com/x");
        _s3.Received(1).GetPreSignedURL(Arg.Is<GetPreSignedUrlRequest>(r =>
            r!.BucketName == "mechanic-test" &&
            r.Key == "folder/file.png" &&
            r.Verb == HttpVerb.GET &&
            r.Expires >= before.AddMinutes(15).AddSeconds(-5) &&
            r.Expires <= DateTime.UtcNow.AddMinutes(15).AddSeconds(5)));
    }

    [Fact]
    public async Task GetPresignedUrlAsync_WithExplicitExpiry_UsesRequestedExpiryAndTrimsLeadingSlash()
    {
        _s3.GetPreSignedURL(Arg.Any<GetPreSignedUrlRequest>()).Returns("https://signed.example.com/y");
        var sut = CreateSut();
        var expiry = TimeSpan.FromMinutes(30);
        var before = DateTime.UtcNow;

        var url = await sut.GetPresignedUrlAsync("/folder/file.png", expiry);

        url.ShouldBe("https://signed.example.com/y");
        _s3.Received(1).GetPreSignedURL(Arg.Is<GetPreSignedUrlRequest>(r =>
            r!.BucketName == "mechanic-test" &&
            r.Key == "folder/file.png" &&
            r.Verb == HttpVerb.GET &&
            r.Expires >= before.Add(expiry).AddSeconds(-5) &&
            r.Expires <= DateTime.UtcNow.Add(expiry).AddSeconds(5)));
    }
}
