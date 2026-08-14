using System.Net;
using System.Xml.Linq;
using Infrastructure.DataProtection.Repositories;
using StackExchange.Redis;

namespace Tests.Infrastructure.DataProtection.Repositories;

public class ResilientRedisXmlRepositoryTests
{
    private const string KeyPrefix = "dp-keys";

    private static readonly TimeSpan Expiration = TimeSpan.FromDays(30);

    private readonly IConnectionMultiplexer _redis = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly IServer _server = Substitute.For<IServer>();
    private readonly ILogger<ResilientRedisXmlRepository> _logger = Substitute.For<ILogger<ResilientRedisXmlRepository>>();

    private readonly EndPoint _endpoint = new DnsEndPoint("localhost", 6379);

    private ResilientRedisXmlRepository CreateSut()
    {
        _redis.GetDatabase().Returns(_database);
        _redis.GetEndPoints().Returns(new[] { _endpoint });
        _redis.GetServer(Arg.Any<EndPoint>()).Returns(_server);
        return new ResilientRedisXmlRepository(_redis, _logger, KeyPrefix, Expiration);
    }

    [Fact]
    public void GetAllElements_WhenRedisDisconnected_ReturnsEmptyCollection()
    {
        _redis.IsConnected.Returns(false);
        var sut = CreateSut();

        var elements = sut.GetAllElements();

        elements.ShouldBeEmpty();
        _redis.DidNotReceive().GetDatabase();
        _redis.DidNotReceive().GetServer(Arg.Any<EndPoint>());
    }

    [Fact]
    public void GetAllElements_WhenNoKeysMatchPattern_ReturnsEmptyCollection()
    {
        _redis.IsConnected.Returns(true);
        _server.Keys(pattern: Arg.Any<RedisValue>()).Returns(Array.Empty<RedisKey>());
        var sut = CreateSut();

        var elements = sut.GetAllElements();

        elements.ShouldBeEmpty();
        _database.DidNotReceive().StringGet(Arg.Any<RedisKey[]>());
    }

    [Fact]
    public void GetAllElements_WhenKeysExist_ReturnsParsedXElementsForEachValue()
    {
        _redis.IsConnected.Returns(true);
        var keys = new RedisKey[] { $"{KeyPrefix}:key-1", $"{KeyPrefix}:key-2" };
        var values = new RedisValue[]
        {
        new XElement("first", new XAttribute("id", "1")).ToString(),
        new XElement("second", new XAttribute("id", "2")).ToString()
        };
        _server.Keys(pattern: Arg.Any<RedisValue>()).Returns(keys);
        _database.StringGet(Arg.Any<RedisKey[]>()).Returns(values);
        var sut = CreateSut();

        var elements = sut.GetAllElements();

        elements.Count.ShouldBe(2);
        var names = elements.Select(e => e.Name.LocalName).ToArray();
        names.ShouldContain("first");
        names.ShouldContain("second");
    }

    [Fact]
    public void GetAllElements_QueriesKeysUsingKeyPrefixWildcardPattern()
    {
        _redis.IsConnected.Returns(true);
        var expectedPattern = $"{KeyPrefix}:*";
        _server
            .Keys(pattern: Arg.Is<RedisValue>(p => p.ToString() == expectedPattern))
            .Returns(new RedisKey[] { $"{KeyPrefix}:only" });
        _database.StringGet(Arg.Any<RedisKey[]>()).Returns(
            new RedisValue[] { new XElement("only").ToString() });
        var sut = CreateSut();

        var elements = sut.GetAllElements();

        elements.Count.ShouldBe(1);
        elements.Single().Name.LocalName.ShouldBe("only");
    }

    [Fact]
    public void GetAllElements_WhenSomeValuesFailToParse_SkipsInvalidAndReturnsRest()
    {
        _redis.IsConnected.Returns(true);
        var keys = new RedisKey[]
        {
        $"{KeyPrefix}:valid-1",
        $"{KeyPrefix}:broken",
        $"{KeyPrefix}:valid-2"
        };
        var values = new RedisValue[]
        {
        new XElement("valid-1").ToString(),
        "<this-is-not-well-formed-xml",
        new XElement("valid-2").ToString()
        };
        _server.Keys(pattern: Arg.Any<RedisValue>()).Returns(keys);
        _database.StringGet(Arg.Any<RedisKey[]>()).Returns(values);
        var sut = CreateSut();

        var elements = sut.GetAllElements();

        elements.Count.ShouldBe(2);
        var names = elements.Select(e => e.Name.LocalName).ToArray();
        names.ShouldContain("valid-1");
        names.ShouldContain("valid-2");
        names.ShouldNotContain("broken");
    }

    [Fact]
    public void GetAllElements_WhenValueHasNoData_SkipsThatEntry()
    {
        _redis.IsConnected.Returns(true);
        var keys = new RedisKey[]
        {
        $"{KeyPrefix}:present",
        $"{KeyPrefix}:missing"
        };
        var values = new RedisValue[]
        {
        new XElement("present").ToString(),
        RedisValue.Null
        };
        _server.Keys(pattern: Arg.Any<RedisValue>()).Returns(keys);
        _database.StringGet(Arg.Any<RedisKey[]>()).Returns(values);
        var sut = CreateSut();

        var elements = sut.GetAllElements();

        elements.Count.ShouldBe(1);
        elements.Single().Name.LocalName.ShouldBe("present");
    }

    [Fact]
    public void GetAllElements_WhenRedisOperationThrows_DoesNotPropagate_ReturnsEmptyCollection()
    {
        _redis.IsConnected.Returns(true);
        _redis.GetEndPoints().Returns(new[] { _endpoint });
        _redis.GetServer(Arg.Any<EndPoint>()).Returns(_server);
        _redis.GetDatabase().Returns(_database);
        _server.Keys(pattern: Arg.Any<RedisValue>()).Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "boom"));
        var sut = new ResilientRedisXmlRepository(_redis, _logger, KeyPrefix, Expiration);

        IReadOnlyCollection<XElement>? elements = null;
        Should.NotThrow(() => elements = sut.GetAllElements());

        elements.ShouldNotBeNull();
        elements!.ShouldBeEmpty();
    }

    [Fact]
    public void StoreElement_WhenRedisDisconnected_DoesNotCallStringSetAndDoesNotThrow()
    {
        _redis.IsConnected.Returns(false);
        var sut = CreateSut();

        Should.NotThrow(() => sut.StoreElement(new XElement("payload"), "friendly"));

        _database.DidNotReceiveWithAnyArgs().StringSet(
            default(RedisKey),
            default(RedisValue),
            default(TimeSpan?),
            default(When),
            default(CommandFlags));
    }

    [Fact]
    public void StoreElement_WhenConnected_CallsStringSetWithComposedKeyValueAndExpiration()
    {
        _redis.IsConnected.Returns(true);
        var sut = CreateSut();
        var element = new XElement("payload", new XAttribute("k", "v"));
        var friendlyName = "master-key";
        var expectedKey = $"{KeyPrefix}:{friendlyName}";
        var expectedValue = element.ToString();

        sut.StoreElement(element, friendlyName);

        _database.Received(1).StringSet(
            Arg.Is<RedisKey>(k => k.ToString() == expectedKey),
            Arg.Is<RedisValue>(v => v.ToString() == expectedValue),
            Arg.Is<TimeSpan?>(t => t.HasValue && t.Value == Expiration),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public void StoreElement_WhenStringSetThrows_DoesNotPropagate()
    {
        _redis.IsConnected.Returns(true);
        var sut = CreateSut();
        _database
            .StringSet(
                Arg.Any<RedisKey>(),
                Arg.Any<RedisValue>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<When>(),
                Arg.Any<CommandFlags>())
            .Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "boom"));

        Should.NotThrow(() => sut.StoreElement(new XElement("payload"), "friendly"));
    }
}
