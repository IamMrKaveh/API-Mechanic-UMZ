using Infrastructure.Cache.Redis.Lock;
using StackExchange.Redis;

namespace Tests.Infrastructure.Cache.Redis.Lock;

public class RedisLockHandleTests
{
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private RedisLockHandle BuildSut(string key = "lock:order:1", string value = "token-abc") =>
        new(_db, key, value, _auditService);

    [Fact]
    public void Resource_ReturnsKey()
    {
        BuildSut("lock:res", "tok").Resource.ShouldBe("lock:res");
    }

    [Fact]
    public void IsAcquired_BeforeRelease_ReturnsTrue()
    {
        BuildSut().IsAcquired.ShouldBeTrue();
    }

    [Fact]
    public async Task ReleaseAsync_EvaluatesLuaScriptWithKeyAndTokenAndLogsDebug()
    {
        var sut = BuildSut("lock:order:7", "tok-7");

        await sut.ReleaseAsync();

        await _db.Received(1).ScriptEvaluateAsync(
            Arg.Is<string>(s => s!.Contains("redis.call('del'")),
            Arg.Is<RedisKey[]>(keys => keys.Length == 1 && keys[0].ToString() == "lock:order:7"),
            Arg.Is<RedisValue[]>(values => values.Length == 1 && values[0].ToString() == "tok-7"),
            Arg.Any<CommandFlags>());
        await _auditService.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains("lock:order:7")),
            Arg.Any<CancellationToken>());
        sut.IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public async Task ReleaseAsync_CalledTwice_EvaluatesScriptOnce()
    {
        var sut = BuildSut();

        await sut.ReleaseAsync();
        await sut.ReleaseAsync();

        await _db.Received(1).ScriptEvaluateAsync(
            Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ReleaseAsync_WhenRedisThrows_LogsErrorAndDoesNotPropagate()
    {
        _db.ScriptEvaluateAsync(
                Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));
        var sut = BuildSut("lock:k");

        await sut.ReleaseAsync();

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("lock:k")),
            Arg.Any<CancellationToken>());
        sut.IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeAsync_ReleasesLock()
    {
        var sut = BuildSut();

        await sut.DisposeAsync();

        await _db.Received(1).ScriptEvaluateAsync(
            Arg.Any<string>(), Arg.Any<RedisKey[]>(), Arg.Any<RedisValue[]>(), Arg.Any<CommandFlags>());
        sut.IsAcquired.ShouldBeFalse();
    }
}
