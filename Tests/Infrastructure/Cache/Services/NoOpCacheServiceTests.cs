using Application.Audit.Contracts;
using Infrastructure.Cache.Services;

namespace Tests.Infrastructure.Cache.Services;

public class NoOpCacheServiceTests : HandlerTestBase
{
    private readonly NoOpCacheService _sut;

    public NoOpCacheServiceTests()
    {
        _sut = new NoOpCacheService(AuditService);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefaultAndLogsDebug()
    {
        var result = await _sut.GetAsync<string>("k");

        result.ShouldBeNull();
        await AuditService.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains("GetAsync") && s!.Contains("k")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAsync_WithValueType_ReturnsDefaultValue()
    {
        var result = await _sut.GetAsync<int>("k");

        result.ShouldBe(0);
    }

    [Fact]
    public async Task SetAsync_DoesNothingAndLogsDebug()
    {
        await _sut.SetAsync("k", "value", TimeSpan.FromMinutes(1));

        await AuditService.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains("SetAsync") && s!.Contains("k")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveAsync_DoesNothingAndLogsDebug()
    {
        await _sut.RemoveAsync("k");

        await AuditService.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains("RemoveAsync") && s!.Contains('k')),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveByPrefixAsync_DoesNothingAndLogsDebug()
    {
        await _sut.RemoveByPrefixAsync("prefix:");

        await AuditService.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains("RemoveByPrefixAsync") && s!.Contains("prefix:")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseAndLogsDebug()
    {
        var result = await _sut.ExistsAsync("k");

        result.ShouldBeFalse();
        await AuditService.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains("ExistsAsync") && s!.Contains('k')),
            Arg.Any<CancellationToken>());
    }
}
