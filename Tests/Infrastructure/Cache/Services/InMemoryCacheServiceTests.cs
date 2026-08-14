using Infrastructure.Cache.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Tests.Infrastructure.Cache.Services;

public class InMemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions()); private readonly InMemoryCacheService _sut;

    public InMemoryCacheServiceTests()
    {
        _sut = new InMemoryCacheService(_memoryCache);
    }

    public void Dispose() => _memoryCache.Dispose();

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsDefault()
    {
        var result = await _sut.GetAsync<string>("missing");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        await _sut.SetAsync("k1", "value-1");

        var result = await _sut.GetAsync<string>("k1");

        result.ShouldBe("value-1");
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_RoundTripsReferenceEquality()
    {
        var payload = new Payload("hello", 42);

        await _sut.SetAsync("k-obj", payload, TimeSpan.FromMinutes(5));
        var result = await _sut.GetAsync<Payload>("k-obj");

        result.ShouldNotBeNull();
        result.ShouldBeSameAs(payload);
    }

    [Fact]
    public async Task RemoveAsync_AfterSet_MakesKeyDisappear()
    {
        await _sut.SetAsync("k2", "value-2");

        await _sut.RemoveAsync("k2");
        var result = await _sut.GetAsync<string>("k2");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyIsSet_ReturnsTrue()
    {
        await _sut.SetAsync("k3", "value-3");

        var exists = await _sut.ExistsAsync("k3");

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyIsMissing_ReturnsFalse()
    {
        var exists = await _sut.ExistsAsync("nope");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveByPrefixAsync_RemovesAllMatchingKeysAndPreservesOthers()
    {
        await _sut.SetAsync("orders:1", "a");
        await _sut.SetAsync("orders:2", "b");
        await _sut.SetAsync("products:1", "c");

        await _sut.RemoveByPrefixAsync("orders:");

        (await _sut.GetAsync<string>("orders:1")).ShouldBeNull();
        (await _sut.GetAsync<string>("orders:2")).ShouldBeNull();
        (await _sut.GetAsync<string>("products:1")).ShouldBe("c");
    }

    [Fact]
    public async Task RemoveByPrefixAsync_MatchesPrefixCaseInsensitively()
    {
        await _sut.SetAsync("Orders:1", "a");
        await _sut.SetAsync("ORDERS:2", "b");

        await _sut.RemoveByPrefixAsync("orders:");

        (await _sut.GetAsync<string>("Orders:1")).ShouldBeNull();
        (await _sut.GetAsync<string>("ORDERS:2")).ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_WithoutExpiry_KeyIsRetrievableImmediately()
    {
        await _sut.SetAsync("k-default", "value");

        var result = await _sut.GetAsync<string>("k-default");

        result.ShouldBe("value");
    }

    private sealed record Payload(string Text, int Number);
}
