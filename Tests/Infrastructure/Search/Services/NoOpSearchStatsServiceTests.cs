using Infrastructure.Search.Services;

namespace Tests.Infrastructure.Search.Services;

public class NoOpSearchStatsServiceTests
{
    private readonly NoOpSearchStatsService _sut = new();

    [Fact]
    public async Task GetStatsAsync_ReturnsUnavailableWithPersianReason()
    {
        var result = await _sut.GetStatsAsync(CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsAvailable.ShouldBeFalse();
        result.UnavailableReason.ShouldBe("سرویس جستجو غیرفعال است.");
    }

    [Fact]
    public async Task GetStatsAsync_ReturnsDefaultCounters()
    {
        var result = await _sut.GetStatsAsync(CancellationToken.None);

        result.Status.ShouldBeNull();
        result.TotalDocuments.ShouldBe(0);
        result.ClusterName.ShouldBeNull();
        result.NumberOfNodes.ShouldBe(0);
        result.ActivePrimaryShards.ShouldBe(0);
    }

    [Fact]
    public async Task GetStatsAsync_HonoursCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _sut.GetStatsAsync(cts.Token);

        result.IsAvailable.ShouldBeFalse();
    }
}
