using Infrastructure.Security.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Tests.Infrastructure.Security.Services;

public class InMemoryRateLimitServiceTests : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions()); private readonly InMemoryRateLimitService _sut;

    public InMemoryRateLimitServiceTests()
    {
        _sut = new InMemoryRateLimitService(_cache);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public async Task IsLimitedAsync_FirstAttemptForKey_ReturnsNotLimited()
    {
        var result = await _sut.IsLimitedAsync("user:1", maxAttempts: 5, windowMinutes: 15);

        result.IsLimited.ShouldBeFalse();
        result.RetryAfterSeconds.ShouldBeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task IsLimitedAsync_AttemptsBelowMax_ReturnsNotLimited(int attemptsBeforeCheck)
    {
        const int maxAttempts = 5;

        for (var i = 0; i < attemptsBeforeCheck; i++)
        {
            await _sut.IsLimitedAsync("user:below-max", maxAttempts, 15);
        }

        var result = await _sut.IsLimitedAsync("user:below-max", maxAttempts, 15);

        result.IsLimited.ShouldBeFalse();
        result.RetryAfterSeconds.ShouldBeNull();
    }

    [Fact]
    public async Task IsLimitedAsync_AttemptsReachMax_ReturnsLimitedWithPositiveRetryAfter()
    {
        const int maxAttempts = 3;
        const int windowMinutes = 15;

        for (var i = 0; i < maxAttempts; i++)
        {
            var interim = await _sut.IsLimitedAsync("user:at-max", maxAttempts, windowMinutes);
            interim.IsLimited.ShouldBeFalse();
        }

        var result = await _sut.IsLimitedAsync("user:at-max", maxAttempts, windowMinutes);

        result.IsLimited.ShouldBeTrue();
        result.RetryAfterSeconds.ShouldNotBeNull();
        result.RetryAfterSeconds!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        result.RetryAfterSeconds!.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(windowMinutes));
    }

    [Fact]
    public async Task IsLimitedAsync_ExceedsMaxAcrossManyAttempts_StaysLimited()
    {
        const int maxAttempts = 2;
        const int windowMinutes = 15;

        await _sut.IsLimitedAsync("user:exceed", maxAttempts, windowMinutes);
        await _sut.IsLimitedAsync("user:exceed", maxAttempts, windowMinutes);

        var third = await _sut.IsLimitedAsync("user:exceed", maxAttempts, windowMinutes);
        var fourth = await _sut.IsLimitedAsync("user:exceed", maxAttempts, windowMinutes);

        third.IsLimited.ShouldBeTrue();
        fourth.IsLimited.ShouldBeTrue();
    }

    [Fact]
    public async Task IsLimitedAsync_DifferentKeys_TrackedIndependently()
    {
        const int maxAttempts = 2;
        const int windowMinutes = 15;

        await _sut.IsLimitedAsync("user:a", maxAttempts, windowMinutes);
        await _sut.IsLimitedAsync("user:a", maxAttempts, windowMinutes);
        var limitedA = await _sut.IsLimitedAsync("user:a", maxAttempts, windowMinutes);
        var freshB = await _sut.IsLimitedAsync("user:b", maxAttempts, windowMinutes);

        limitedA.IsLimited.ShouldBeTrue();
        freshB.IsLimited.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public async Task IsLimitedAsync_WithCustomMaxAttempts_LimitsAtConfiguredThreshold(int maxAttempts)
    {
        const int windowMinutes = 5;
        var key = $"user:custom-{maxAttempts}";

        for (var i = 0; i < maxAttempts; i++)
        {
            var interim = await _sut.IsLimitedAsync(key, maxAttempts, windowMinutes);
            interim.IsLimited.ShouldBeFalse();
        }

        var overLimit = await _sut.IsLimitedAsync(key, maxAttempts, windowMinutes);

        overLimit.IsLimited.ShouldBeTrue();
        overLimit.RetryAfterSeconds.ShouldNotBeNull();
    }

    [Fact]
    public async Task IsLimitedAsync_UsingDefaultParameters_LimitsAtFiveAttempts()
    {
        for (var i = 0; i < 5; i++)
        {
            var interim = await _sut.IsLimitedAsync("user:defaults");
            interim.IsLimited.ShouldBeFalse();
        }

        var result = await _sut.IsLimitedAsync("user:defaults");

        result.IsLimited.ShouldBeTrue();
        result.RetryAfterSeconds.ShouldNotBeNull();
        result.RetryAfterSeconds!.Value.ShouldBeLessThanOrEqualTo(TimeSpan.FromMinutes(15));
    }
}
