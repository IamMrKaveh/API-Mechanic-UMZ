using Infrastructure.Storage.Services;

namespace Tests.Infrastructure.Storage.Services;

public class NullFileScanningServiceTests
{
    private readonly NullFileScanningService _sut = new();

    [Fact]
    public async Task ScanAsync_WithNonEmptyStream_ReturnsClean()
    {
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });

        var result = await _sut.ScanAsync(stream, "any.bin");

        result.ShouldNotBeNull();
        result.IsClean.ShouldBeTrue();
        result.ThreatName.ShouldBeNull();
        result.EngineMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ScanAsync_WithEmptyStream_ReturnsClean()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());

        var result = await _sut.ScanAsync(stream, "empty.bin");

        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public async Task ScanAsync_WithNullStream_ReturnsCleanWithoutThrowing()
    {
        var result = await _sut.ScanAsync(null!, "unused.bin");

        result.IsClean.ShouldBeTrue();
    }

    [Fact]
    public async Task ScanAsync_WithCancelledToken_ReturnsCleanWithoutThrowing()
    {
        using var stream = new MemoryStream(new byte[] { 0x00 });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.ScanAsync(stream, "any.bin", cts.Token);

        result.IsClean.ShouldBeTrue();
    }
}
