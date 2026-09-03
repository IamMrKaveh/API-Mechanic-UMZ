using Infrastructure.Cache.Options;
using Infrastructure.Cache.Redis.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tests.Infrastructure.Cache.Redis.Services;

public class RedisIdempotencyServiceTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly ILogger<RedisIdempotencyService> _logger = Substitute.For<ILogger<RedisIdempotencyService>>();

    public RedisIdempotencyServiceTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
    }

    private RedisIdempotencyService BuildSut(string keyPrefix = "shop") =>
        new(_redis, Options.Create(new CacheOptions { KeyPrefix = keyPrefix }), _logger);

    private static string ExpectedKey(string prefix, Guid key) => $"{prefix}:idem:{key:N}";

    private void StubKeyExists(bool value)
    {
        Func<RedisKey, CommandFlags, Task<bool>> keyExists = _db.KeyExistsAsync;
        keyExists(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(value);
    }

    private void StubKeyExistsThrows(Exception ex)
    {
        Func<RedisKey, CommandFlags, Task<bool>> keyExists = _db.KeyExistsAsync;
        keyExists(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Throws(ex);
    }

    private void StubStringGet(RedisValue value)
    {
        Func<RedisKey, CommandFlags, Task<RedisValue>> stringGet = _db.StringGetAsync;
        stringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(value);
    }

    private void StubStringGetThrows(Exception ex)
    {
        Func<RedisKey, CommandFlags, Task<RedisValue>> stringGet = _db.StringGetAsync;
        stringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Throws(ex);
    }

    private static RedisConnectionException RedisDown() =>
        new(ConnectionFailureType.UnableToConnect, "down");

    [Fact]
    public async Task HasBeenProcessedAsync_WhenKeyExists_ReturnsTrue()
    {
        var key = Guid.NewGuid();
        var seenKeys = new List<string>();
        Func<RedisKey, CommandFlags, Task<bool>> keyExists = _db.KeyExistsAsync;
        keyExists(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(true)
            .AndDoes(call => seenKeys.Add(call.Arg<RedisKey>().ToString()));

        var result = await BuildSut().HasBeenProcessedAsync(key);

        result.ShouldBeTrue();
        seenKeys.ShouldBe([ExpectedKey("shop", key)]);
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenKeyIsMissing_ReturnsFalse()
    {
        StubKeyExists(false);

        var result = await BuildSut().HasBeenProcessedAsync(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenKeyIsEmpty_ReturnsFalseWithoutRedisCall()
    {
        var result = await BuildSut().HasBeenProcessedAsync(Guid.Empty);

        result.ShouldBeFalse();
        await _db.DidNotReceiveWithAnyArgs().KeyExistsAsync(default(RedisKey), default);
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenRedisThrows_ReturnsFalse()
    {
        StubKeyExistsThrows(RedisDown());

        var result = await BuildSut().HasBeenProcessedAsync(Guid.NewGuid());

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_StoresEnvelopeWithNotExistsAnd24HourTtl()
    {
        var key = Guid.NewGuid();
        var sut = BuildSut();

        await sut.MarkAsProcessedAsync(key, "{\"orderId\":1}");

        await _db.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == ExpectedKey("shop", key)),
            Arg.Is<RedisValue>(v => v.ToString().Contains("orderId")),
            Arg.Is<TimeSpan?>(e => e == TimeSpan.FromHours(24)),
            Arg.Is<When>(w => w == When.NotExists));
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WhenKeyIsEmpty_DoesNotTouchRedis()
    {
        await BuildSut().MarkAsProcessedAsync(Guid.Empty, "result");

        await _db.DidNotReceiveWithAnyArgs().StringSetAsync(
            default(RedisKey), default(RedisValue), default(TimeSpan?), default(When), default);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_WhenRedisThrows_DoesNotPropagate()
    {
        Func<RedisKey, RedisValue, TimeSpan?, When, Task<bool>> stringSet = _db.StringSetAsync;
        stringSet(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(),
                Arg.Any<When>())
            .Throws(RedisDown());

        await BuildSut().MarkAsProcessedAsync(Guid.NewGuid(), "result");
    }

    [Fact]
    public async Task GetResultAsync_WhenEnvelopeExists_ReturnsStoredResult()
    {
        var key = Guid.NewGuid();
        var seenKeys = new List<string>();
        Func<RedisKey, CommandFlags, Task<RedisValue>> stringGet = _db.StringGetAsync;
        stringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns("{\"result\":\"done-42\",\"processedAt\":\"2026-01-01T00:00:00Z\"}")
            .AndDoes(call => seenKeys.Add(call.Arg<RedisKey>().ToString()));

        var result = await BuildSut().GetResultAsync(key);

        result.ShouldBe("done-42");
        seenKeys.ShouldBe([ExpectedKey("shop", key)]);
    }

    [Fact]
    public async Task GetResultAsync_WhenKeyIsMissing_ReturnsNull()
    {
        StubStringGet(RedisValue.Null);

        var result = await BuildSut().GetResultAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetResultAsync_WhenKeyIsEmpty_ReturnsNullWithoutRedisCall()
    {
        var result = await BuildSut().GetResultAsync(Guid.Empty);

        result.ShouldBeNull();
        await _db.DidNotReceiveWithAnyArgs().StringGetAsync(default(RedisKey), default);
    }

    [Fact]
    public async Task GetResultAsync_WhenRedisThrows_ReturnsNull()
    {
        StubStringGetThrows(RedisDown());

        var result = await BuildSut().GetResultAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task KeyPrefix_WhenCustomPrefix_UsesItForAllOperations()
    {
        var key = Guid.NewGuid();
        var sut = BuildSut(keyPrefix: "myapp");
        var seenKeys = new List<string>();
        Func<RedisKey, CommandFlags, Task<bool>> keyExists = _db.KeyExistsAsync;
        keyExists(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(false)
            .AndDoes(call => seenKeys.Add(call.Arg<RedisKey>().ToString()));

        await sut.HasBeenProcessedAsync(key);

        seenKeys.ShouldBe([ExpectedKey("myapp", key)]);
    }
}
