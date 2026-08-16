using Application.Audit.Contracts;
using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.Storage.Services;

public class ClamAvFileScanningServiceTests
{
    private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private static IOptions<AntivirusOptions> Options(AntivirusOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    [Fact]
    public async Task ScanAsync_WhenAntivirusIsDisabled_ReturnsCleanWithoutContactingEngine()
    {
        var sut = new ClamAvFileScanningService(
            Options(new AntivirusOptions { IsEnabled = false }),
            _audit);

        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 });

        var result = await sut.ScanAsync(stream, "any.bin");

        result.ShouldNotBeNull();
        result.IsClean.ShouldBeTrue();
        result.ThreatName.ShouldBeNull();
        result.EngineMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ScanAsync_WhenAntivirusIsDisabled_DoesNotWriteToAuditService()
    {
        var sut = new ClamAvFileScanningService(
            Options(new AntivirusOptions { IsEnabled = false }),
            _audit);

        using var stream = new MemoryStream(new byte[] { 0x00 });

        await sut.ScanAsync(stream, "any.bin");

        await _audit.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
        await _audit.DidNotReceiveWithAnyArgs().LogSecurityEventAsync(default!, default!, default!, default, default);
        await _audit.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task ScanAsync_WhenAntivirusIsEnabledAndStreamIsNull_ThrowsArgumentNullException()
    {
        var sut = new ClamAvFileScanningService(
            Options(new AntivirusOptions
            {
                IsEnabled = true,
                Host = "127.0.0.1",
                Port = 3310,
                TimeoutSeconds = 1,
                ChunkSizeBytes = 1024,
                FailClosedOnEngineError = true
            }),
            _audit);

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ScanAsync(null!, "any.bin"));

        ex.ParamName.ShouldBe("stream");
    }

    [Fact]
    public async Task ScanAsync_WhenAntivirusIsEnabledAndStreamIsNull_DoesNotWriteToAuditService()
    {
        var sut = new ClamAvFileScanningService(
            Options(new AntivirusOptions
            {
                IsEnabled = true,
                Host = "127.0.0.1",
                Port = 3310,
                TimeoutSeconds = 1,
                ChunkSizeBytes = 1024,
                FailClosedOnEngineError = true
            }),
            _audit);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ScanAsync(null!, "any.bin"));

        await _audit.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
        await _audit.DidNotReceiveWithAnyArgs().LogSecurityEventAsync(default!, default!, default!, default, default);
    }
}
