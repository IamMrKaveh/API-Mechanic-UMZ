using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class ForwardedHeadersExtensionsTests
{
    private static IConfiguration BuildConfig(
        string[]? proxies = null,
        string[]? networks = null)
    {
        var data = new List<KeyValuePair<string, string?>>();
        (proxies ?? []).Select((p, i) =>
            new KeyValuePair<string, string?>($"ReverseProxy:TrustedProxies:{i}", p))
            .ToList().ForEach(data.Add);
        (networks ?? []).Select((n, i) =>
            new KeyValuePair<string, string?>($"ReverseProxy:TrustedNetworks:{i}", n))
            .ToList().ForEach(data.Add);
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void AddTrustedForwardedHeaders_SetsForwardedHeadersFlags()
    {
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(BuildConfig());

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.ForwardedHeaders.ShouldBe(
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
    }

    [Fact]
    public void AddTrustedForwardedHeaders_WithValidProxy_AddsKnownProxy()
    {
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(BuildConfig(proxies: ["10.0.0.5", "not-an-ip"]));

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.KnownProxies.Count.ShouldBe(1);
        options.KnownProxies.ShouldContain(IPAddress.Parse("10.0.0.5"));
    }

    [Fact]
    public void AddTrustedForwardedHeaders_WithValidNetwork_AddsKnownNetwork()
    {
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(BuildConfig(networks: ["10.0.0.0/8", "bad-network"]));

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.KnownNetworks.Count.ShouldBe(1);
        options.KnownNetworks.First().Prefix.ToString().ShouldBe("10.0.0.0");
        options.KnownNetworks.First().PrefixLength.ShouldBe(8);
    }

    [Fact]
    public void AddTrustedForwardedHeaders_WithoutConfig_LeavesListsEmpty()
    {
        var services = new ServiceCollection();
        services.AddTrustedForwardedHeaders(BuildConfig());

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        options.KnownProxies.Count.ShouldBe(0);
        options.KnownNetworks.Count.ShouldBe(0);
    }

    [Fact]
    public void AddTrustedForwardedHeaders_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        services.AddTrustedForwardedHeaders(BuildConfig()).ShouldBeSameAs(services);
    }
}
