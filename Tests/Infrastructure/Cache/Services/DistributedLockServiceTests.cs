using Infrastructure.Cache.Redis.Lock;
using Infrastructure.Cache.Services;
using StackExchange.Redis;

namespace Tests.Infrastructure.Cache.Services;

public class DistributedLockServiceTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly DistributedLockService _sut;

    public DistributedLockServiceTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
        _sut = new DistributedLockService(_redis, _auditService);
    }

    [Fact]
    public async Task AcquireAsync_WhenLockIsFree_ReturnsHandleWithResource()
    {
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);

        var handle = await _sut.AcquireAsync("order:123", TimeSpan.FromSeconds(30));

        handle.ShouldNotBeNull();
        handle!.Resource.ShouldBe("lock:order:123");
        handle.IsAcquired.ShouldBeTrue();
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task AcquireAsync_UsesNotExistsWithGivenExpiry()
    {
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);
        var expiry = TimeSpan.FromSeconds(45);

        await _sut.AcquireAsync("order:123", expiry);

        await _db.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "lock:order:123"),
            Arg.Is<RedisValue>(v => v.HasValue && v.ToString().Length == 32),
            Arg.Is<TimeSpan?>(e => e == expiry),
            Arg.Is<When>(w => w == When.NotExists));
    }

    [Fact]
    public async Task AcquireAsync_WhenLockIsHeld_ReturnsNull()
    {
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(false);

        var handle = await _sut.AcquireAsync("order:123", TimeSpan.FromSeconds(30));

        handle.ShouldBeNull();
    }

    [Fact]
    public async Task AcquireAsync_GeneratesUniqueTokensPerCall()
    {
        var tokens = new List<string>();
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true)
            .AndDoes(call => tokens.Add(call.ArgAt<RedisValue>(1).ToString()));

        await _sut.AcquireAsync("res", TimeSpan.FromSeconds(10));
        await _sut.AcquireAsync("res", TimeSpan.FromSeconds(10));

        tokens.Count.ShouldBe(2);
        tokens[0].ShouldNotBe(tokens[1]);
    }

    [Fact]
    public async Task AcquiredHandle_ReleaseAsync_DeletesKeyOnlyWithMatchingToken()
    {
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);

        var handle = await _sut.AcquireAsync("order:9", TimeSpan.FromSeconds(30));
        handle.ShouldNotBeNull();
        await handle!.ReleaseAsync();

        await _db.Received(1).ScriptEvaluateAsync(
            Arg.Is<string>(s => s!.Contains("redis.call('del'")),
            Arg.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0].ToString() == "lock:order:9"),
            Arg.Is<RedisValue[]>(values => values.Length == 1 && values[0].ToString().Length == 32),
            Arg.Any<CommandFlags>());
        handle.IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public async Task AcquiredHandle_ReleaseAsync_IsIdempotent()
    {
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<TimeSpan?>(), Arg.Any<When>())
            .Returns(true);

        var handle = await _sut.AcquireAsync("order:9", TimeSpan.FromSeconds(30));
        handle.ShouldNotBeNull();
        await handle!.ReleaseAsync();
        await handle.ReleaseAsync();

        await _db.Received(1).ScriptEvaluateAsync(
            Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
    }
}
