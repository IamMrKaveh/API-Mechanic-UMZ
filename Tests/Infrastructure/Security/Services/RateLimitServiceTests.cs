using Infrastructure.Security.Services;
using StackExchange.Redis;

namespace Tests.Infrastructure.Security.Services;

public class RateLimitServiceTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly RateLimitService _sut;

    public RateLimitServiceTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
        _sut = new RateLimitService(_redis);
    }

    private static RedisResult ScriptResult(long limited, long ttlMs) =>
        RedisResult.Create(new[]
        {
            RedisResult.Create((RedisValue)limited),
            RedisResult.Create((RedisValue)ttlMs)
        });

    [Fact]
    public async Task IsLimitedAsync_WhenUnderLimit_ReturnsNotLimited()
    {
        _db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(ScriptResult(0, 0));

        var (isLimited, retryAfter) = await _sut.IsLimitedAsync("user:1", maxRequests: 5, windowSeconds: 60);

        isLimited.ShouldBeFalse();
        retryAfter.ShouldBeNull();
    }

    [Fact]
    public async Task IsLimitedAsync_WhenOverLimit_ReturnsLimitedWithTtlRetryAfter()
    {
        _db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(ScriptResult(1, 1500));

        var (isLimited, retryAfter) = await _sut.IsLimitedAsync("user:1", maxRequests: 5, windowSeconds: 60);

        isLimited.ShouldBeTrue();
        retryAfter.ShouldBe(TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public async Task IsLimitedAsync_WhenLimitedWithoutTtl_FallsBackToWindowSeconds()
    {
        _db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(ScriptResult(1, -1));

        var (isLimited, retryAfter) = await _sut.IsLimitedAsync("user:1", maxRequests: 5, windowSeconds: 60);

        isLimited.ShouldBeTrue();
        retryAfter.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task IsLimitedAsync_UsesPrefixedKeyAndPassesWindowParameters()
    {
        RedisKey[]? capturedKeys = null;
        RedisValue[]? capturedValues = null;
        _db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(ScriptResult(0, 0))
            .AndDoes(call =>
            {
                capturedKeys = call.Arg<RedisKey[]>();
                capturedValues = call.Arg<RedisValue[]>();
            });

        await _sut.IsLimitedAsync("user:42", maxRequests: 10, windowSeconds: 30);

        capturedKeys.ShouldNotBeNull();
        capturedKeys!.Length.ShouldBe(1);
        capturedKeys[0].ToString().ShouldBe("ratelimit:user:42");
        capturedValues.ShouldNotBeNull();
        capturedValues!.Length.ShouldBe(4);
        ((long)capturedValues[1]).ShouldBe(30);
        ((long)capturedValues[2]).ShouldBe(10);
    }

    [Fact]
    public async Task IsLimitedAsync_SendsSlidingWindowScript()
    {
        string? capturedScript = null;
        _db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Returns(ScriptResult(0, 0))
            .AndDoes(call => capturedScript = call.Arg<string>());

        await _sut.IsLimitedAsync("k", 5, 60);

        capturedScript.ShouldNotBeNull();
        capturedScript!.ShouldContain("ZREMRANGEBYSCORE");
        capturedScript.ShouldContain("ZCARD");
        capturedScript.ShouldContain("ZADD");
    }

    [Fact]
    public async Task ResetAsync_DeletesPrefixedKey()
    {
        _db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);

        await _sut.ResetAsync("user:42");

        await _db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "ratelimit:user:42"),
            Arg.Any<CommandFlags>());
    }
}
