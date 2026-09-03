using Infrastructure.Cache.Options;
using Infrastructure.Cache.Redis.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Tests.Infrastructure.Cache.Redis.Services;

public class RedisCacheServiceTests
{
    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly ILogger<RedisCacheService> _logger = Substitute.For<ILogger<RedisCacheService>>();

    private RedisCacheService BuildSut(string keyPrefix = "shop", int defaultExpirationMinutes = 30) =>
        new(_redis,
            Options.Create(new CacheOptions { KeyPrefix = keyPrefix, DefaultExpirationMinutes = defaultExpirationMinutes }),
            _logger);

    public RedisCacheServiceTests()
    {
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
    }

    private sealed record SampleDto(string Name, int Quantity);

    [Fact]
    public async Task GetAsync_WhenKeyExists_DeserializesWithCamelCase()
    {
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"{\"name\":\"Brake Pad\",\"quantity\":2}");

        var result = await BuildSut().GetAsync<SampleDto>("product:1");

        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Brake Pad");
        result.Quantity.ShouldBe(2);
        await _db.Received(1).StringGetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "shop:product:1"),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task GetAsync_WhenKeyIsMissing_ReturnsDefault()
    {
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        var result = await BuildSut().GetAsync<SampleDto>("missing");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_WhenRedisThrows_ReturnsDefault()
    {
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var result = await BuildSut().GetAsync<SampleDto>("k");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAsync_WhenPayloadIsCorrupt_ReturnsDefault()
    {
        _db.StringGetAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Returns((RedisValue)"not-json{{{");

        var result = await BuildSut().GetAsync<SampleDto>("k");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_SerializesWithPrefixAndDefaultExpiry()
    {
        var sut = BuildSut(keyPrefix: "shop", defaultExpirationMinutes: 30);

        await sut.SetAsync("product:1", new SampleDto("Brake Pad", 2));

        await _db.Received(1).StringSetAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "shop:product:1"),
            Arg.Is<RedisValue>(v => v.ToString().Contains("Brake Pad")),
            Arg.Is<StackExchange.Redis.Expiration>(e => e.Equals((StackExchange.Redis.Expiration)TimeSpan.FromMinutes(30))),
            Arg.Any<StackExchange.Redis.ValueCondition>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WhenExpiryProvided_UsesProvidedExpiry()
    {
        var sut = BuildSut();

        await sut.SetAsync("k", "v", TimeSpan.FromMinutes(5));

        await _db.Received(1).StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Is<StackExchange.Redis.Expiration>(e => e.Equals((StackExchange.Redis.Expiration)TimeSpan.FromMinutes(5))),
            Arg.Any<StackExchange.Redis.ValueCondition>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WhenRedisThrows_DoesNotPropagate()
    {
        _db.StringSetAsync(
                Arg.Any<RedisKey>(), Arg.Any<RedisValue>(), Arg.Any<StackExchange.Redis.Expiration>(), Arg.Any<StackExchange.Redis.ValueCondition>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        await BuildSut().SetAsync("k", "v");
    }

    [Fact]
    public async Task RemoveAsync_DeletesPrefixedKey()
    {
        await BuildSut().RemoveAsync("product:1");

        await _db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "shop:product:1"),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveAsync_WhenRedisThrows_DoesNotPropagate()
    {
        _db.KeyDeleteAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        await BuildSut().RemoveAsync("k");
    }

    [Fact]
    public async Task RemoveByPrefixAsync_DeletesAllMatchingKeys()
    {
        var server = Substitute.For<IServer>();
        server.KeysAsync(Arg.Any<int>(), Arg.Any<RedisValue>(), Arg.Any<int>(), Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
            .Returns(_ => AsyncKeys("shop:product:1", "shop:product:2"));
        _redis.GetEndPoints().Returns([new DnsEndPoint("localhost", 6379)]);
        _redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object?>()).Returns(server);
        _db.KeyDeleteAsync(Arg.Any<RedisKey[]>(), Arg.Any<CommandFlags>()).Returns(2L);

        await BuildSut().RemoveByPrefixAsync("product:");

        await _db.Received(1).KeyDeleteAsync(
            Arg.Is<RedisKey[]>(keys => keys.Length == 2),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveByPrefixAsync_WhenNoKeysMatch_DoesNotDelete()
    {
        var server = Substitute.For<IServer>();
        server.KeysAsync(Arg.Any<int>(), Arg.Any<RedisValue>(), Arg.Any<int>(), Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CommandFlags>())
            .Returns(_ => AsyncKeys());
        _redis.GetEndPoints().Returns([new DnsEndPoint("localhost", 6379)]);
        _redis.GetServer(Arg.Any<EndPoint>(), Arg.Any<object?>()).Returns(server);

        await BuildSut().RemoveByPrefixAsync("nothing:");

        await _db.DidNotReceiveWithAnyArgs().KeyDeleteAsync(default(RedisKey[])!, default);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_WhenRedisThrows_DoesNotPropagate()
    {
        _redis.GetEndPoints().Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        await BuildSut().RemoveByPrefixAsync("product:");
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        _db.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>()).Returns(true);

        var result = await BuildSut().ExistsAsync("k");

        result.ShouldBeTrue();
        await _db.Received(1).KeyExistsAsync(
            Arg.Is<RedisKey>(k => k.ToString() == "shop:k"),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task ExistsAsync_WhenRedisThrows_ReturnsFalse()
    {
        _db.KeyExistsAsync(Arg.Any<RedisKey>(), Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "down"));

        var result = await BuildSut().ExistsAsync("k");

        result.ShouldBeFalse();
    }

    private static async IAsyncEnumerable<RedisKey> AsyncKeys(params string[] keys)
    {
        foreach (var key in keys)
        {
            await Task.Yield();
            yield return (RedisKey)key;
        }
    }
}
