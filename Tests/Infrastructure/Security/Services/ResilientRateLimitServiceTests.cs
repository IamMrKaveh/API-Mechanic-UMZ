using System.Diagnostics.Metrics;
using Infrastructure.Security.Services;
using Microsoft.Extensions.Caching.Memory;
using SharedContracts.Diagnostics;
using StackExchange.Redis;

namespace Tests.Infrastructure.Security.Services;

public class ResilientRateLimitServiceTests : IDisposable
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>(); private readonly IDatabase _redisDatabase = Substitute.For<IDatabase>(); private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions()); private readonly ILogger<ResilientRateLimitService> _logger = Substitute.For<ILogger<ResilientRateLimitService>>(); private readonly IMeterFactory _meterFactory = Substitute.For<IMeterFactory>(); private readonly BusinessMetrics _metrics; private readonly RateLimitService _primary; private readonly InMemoryRateLimitService _fallback; private readonly ResilientRateLimitService _sut;

    public ResilientRateLimitServiceTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_redisDatabase);

        _meterFactory
            .Create(Arg.Any<MeterOptions>())
            .Returns(ci => new Meter(ci.Arg<MeterOptions>()));

        _metrics = new BusinessMetrics(_meterFactory);
        _primary = new RateLimitService(_redis);
        _fallback = new InMemoryRateLimitService(_memoryCache);
        _sut = new ResilientRateLimitService(_primary, _fallback, _metrics, _logger);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _memoryCache.Dispose();
    }

    [Fact]
    public async Task IsLimitedAsync_WhenPrimaryThrows_ReturnsFallbackResult()
    {
        _redisDatabase
            .ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("redis down"));

        var result = await _sut.IsLimitedAsync("user:fallback", maxAttempts: 5, windowUnits: 15);

        result.IsLimited.ShouldBeFalse();
        result.RetryAfterSeconds.ShouldBeNull();
    }

    [Fact]
    public async Task IsLimitedAsync_WhenPrimaryThrowsRepeatedlyUnderThreshold_FallbackReportsNotLimited()
    {
        _redisDatabase
            .ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("redis down"));

        var first = await _sut.IsLimitedAsync("user:fallback-under", 5, 15);
        var second = await _sut.IsLimitedAsync("user:fallback-under", 5, 15);
        var third = await _sut.IsLimitedAsync("user:fallback-under", 5, 15);

        first.IsLimited.ShouldBeFalse();
        second.IsLimited.ShouldBeFalse();
        third.IsLimited.ShouldBeFalse();
    }

    [Fact]
    public async Task IsLimitedAsync_WhenPrimaryThrowsAndFallbackReachesLimit_ReturnsLimited()
    {
        _redisDatabase
            .ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>(),
                Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("redis down"));

        const int maxAttempts = 2;

        var attempt1 = await _sut.IsLimitedAsync("user:fallback-hit", maxAttempts, 15);
        var attempt2 = await _sut.IsLimitedAsync("user:fallback-hit", maxAttempts, 15);
        var attempt3 = await _sut.IsLimitedAsync("user:fallback-hit", maxAttempts, 15);

        attempt1.IsLimited.ShouldBeFalse();
        attempt2.IsLimited.ShouldBeFalse();
        attempt3.IsLimited.ShouldBeTrue();
        attempt3.RetryAfterSeconds.ShouldNotBeNull();
    }

    [Fact]
    public async Task ResetAsync_WhenPrimarySucceeds_InvokesRedisKeyDelete()
    {
        _redisDatabase
            .KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true);

        await _sut.ResetAsync("user:reset");

        await _redisDatabase.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "ratelimit:user:reset"),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ResetAsync_WhenPrimaryThrows_DoesNotPropagateException()
    {
        _redisDatabase
            .KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new InvalidOperationException("redis down"));

        Func<Task> act = async () => await _sut.ResetAsync("user:reset-fail");

        await act.ShouldNotThrowAsync();
    }
}
