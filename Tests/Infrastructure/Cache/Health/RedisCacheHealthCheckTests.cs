using Infrastructure.Cache.Health;
using Infrastructure.Cache.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tests.Infrastructure.Cache.Health;

public class RedisCacheHealthCheckTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public RedisCacheHealthCheckTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
        _redis.GetEndPoints().Returns([new DnsEndPoint("localhost", 6379)]);
        _redis.IsConnected.Returns(true);
    }

    private RedisCacheHealthCheck BuildSut(bool isEnabled = true) =>
        new(_redis, _auditService,
            Options.Create(new CacheOptions { IsEnabled = isEnabled, KeyPrefix = "shop" }));

    private static HealthCheckContext Context() =>
        new() { Registration = new HealthCheckRegistration("redis", _ => null!, null, null) };

    [Fact]
    public async Task CheckHealthAsync_WhenCacheIsDisabled_ReturnsHealthyWithoutTouchingRedis()
    {
        var result = await BuildSut(isEnabled: false).CheckHealthAsync(Context());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("Cache is disabled in configuration");
        await _db.DidNotReceiveWithAnyArgs().PingAsync(default);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisIsHealthy_ReturnsHealthyWithData()
    {
        string? written = null;
        _db.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(4));
        Func<RedisKey, RedisValue, StackExchange.Redis.Expiration, StackExchange.Redis.ValueCondition, CommandFlags, Task<bool>> stringSet = _db.StringSetAsync;
        stringSet(Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<StackExchange.Redis.Expiration>(), Arg.Any<StackExchange.Redis.ValueCondition>(), Arg.Any<CommandFlags>())
            .Returns(true)
            .AndDoes(call => written = call.ArgAt<RedisValue>(1).ToString());
        Func<RedisKey, CommandFlags, Task<RedisValue>> stringGet = _db.StringGetAsync;
        stringGet(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(_ => (RedisValue)written!);

        var result = await BuildSut().CheckHealthAsync(Context());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("Redis is healthy");
        result.Data.ShouldContainKey("PingLatency");
        result.Data.ShouldContainKey("CheckLatency");
        result.Data["ConnectedEndpoints"].ShouldBe(1);
        result.Data["IsConnected"].ShouldBe(true);
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenWriteReadMismatch_ReturnsDegraded()
    {
        _db.PingAsync(Arg.Any<CommandFlags>()).Returns(TimeSpan.FromMilliseconds(4));
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"something-else");

        var result = await BuildSut().CheckHealthAsync(Context());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe("Redis write/read mismatch");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenRedisThrows_ReturnsUnhealthyWithErrorData()
    {
        _db.PingAsync(Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var result = await BuildSut().CheckHealthAsync(Context());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Redis is unreachable");
        result.Exception.ShouldBeOfType<RedisConnectionException>();
        result.Data["Type"].ShouldBe("RedisConnectionException");
        await _auditService.Received(1).LogErrorAsync("Redis health check failed.", Arg.Any<CancellationToken>());
    }
}
