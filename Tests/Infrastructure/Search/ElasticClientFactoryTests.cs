using Infrastructure.Search;
using Infrastructure.Search.Options;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Search;

public class ElasticClientFactoryTests : IAsyncLifetime
{
    private FakeElasticsearchServer _server = null!;

    public Task InitializeAsync()
    {
        _server = new FakeElasticsearchServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    private ElasticsearchOptions OptionsFor(
        string[]? urls = null,
        string username = "",
        string password = "",
        bool debugMode = false,
        int timeoutSeconds = 5,
        int maxRetries = 0) => new()
    {
        Urls = urls ?? [],
        TimeoutSeconds = timeoutSeconds,
        MaxRetries = maxRetries,
        Username = username,
        Password = password,
        DebugMode = debugMode
    };

    [Fact]
    public void Create_WithSingleUrl_ReturnsClient()
    {
        var client = ElasticClientFactory.Create(OptionsFor([_server.BaseUrl]));

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_WithReachableUrl_PingSucceeds()
    {
        var client = ElasticClientFactory.Create(OptionsFor([_server.BaseUrl]));

        (await client.PingAsync()).IsValidResponse.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_WithMultipleUrls_UsesFirstUrl()
    {
        var client = ElasticClientFactory.Create(OptionsFor([_server.BaseUrl, "http://127.0.0.1:9"]));

        (await client.PingAsync()).IsValidResponse.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithEmptyUrls_FallsBackToLocalhostDefault()
    {
        var client = ElasticClientFactory.Create(OptionsFor());

        client.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithBasicAuthCredentials_ReturnsClient()
    {
        var client = ElasticClientFactory.Create(
            OptionsFor([_server.BaseUrl], username: "elastic", password: "secret"));

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task Create_WithBasicAuthCredentials_PingSucceeds()
    {
        var client = ElasticClientFactory.Create(
            OptionsFor([_server.BaseUrl], username: "elastic", password: "secret"));

        (await client.PingAsync()).IsValidResponse.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "secret")]
    [InlineData("elastic", "")]
    [InlineData("", "")]
    [InlineData("   ", "secret")]
    public async Task Create_WithIncompleteCredentials_SkipsAuthentication(string username, string password)
    {
        var client = ElasticClientFactory.Create(
            OptionsFor([_server.BaseUrl], username: username, password: password));

        (await client.PingAsync()).IsValidResponse.ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WithDebugModeEitherWay_PingSucceeds(bool debugMode)
    {
        var client = ElasticClientFactory.Create(
            OptionsFor([_server.BaseUrl], debugMode: debugMode));

        (await client.PingAsync()).IsValidResponse.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_FromIOptionsWrapper_UsesWrappedValue()
    {
        var options = Options.Create(OptionsFor([_server.BaseUrl]));

        var client = ElasticClientFactory.Create(options);

        (await client.PingAsync()).IsValidResponse.ShouldBeTrue();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(30, 3)]
    [InlineData(300, 10)]
    public void Create_WithTimeoutAndRetryBoundaries_ReturnsClient(int timeoutSeconds, int maxRetries)
    {
        var client = ElasticClientFactory.Create(
            OptionsFor([_server.BaseUrl], timeoutSeconds: timeoutSeconds, maxRetries: maxRetries));

        client.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithMalformedUrl_Throws()
    {
        Should.Throw<Exception>(() => ElasticClientFactory.Create(OptionsFor(["ht!tp://[invalid"])));
    }
}
