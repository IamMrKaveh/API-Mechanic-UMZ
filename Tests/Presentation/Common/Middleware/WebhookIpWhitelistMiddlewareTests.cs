using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Presentation.Common.Middleware;
using Presentation.Common.Options;
using SharedContracts.FeatureManagement;

namespace Tests.Presentation.Common.Middleware;

public class WebhookIpWhitelistMiddlewareTests
{
    private readonly ILogger<WebhookIpWhitelistMiddleware> _logger =
        Substitute.For<ILogger<WebhookIpWhitelistMiddleware>>();
    private readonly IFeatureManager _features = Substitute.For<IFeatureManager>();

    private WebhookIpWhitelistMiddleware BuildSut(
        RequestDelegate next,
        string[]? allowedIps = null,
        string[]? allowedPaths = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                (allowedIps ?? ["10.0.0.1"]).Select((ip, i) =>
                    new KeyValuePair<string, string?>($"Zarinpal:AllowedIps:{i}", ip)))
            .Build();
        return new WebhookIpWhitelistMiddleware(
            next,
            config,
            Options.Create(new WebhookOptions
            {
                AllowedPaths = allowedPaths?.ToList() ?? ["/api/payment/callback"]
            }),
            _logger);
    }

    private DefaultHttpContext BuildContext(string path, string ip)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        var services = new ServiceCollection();
        services.AddSingleton(_features);
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NonWebhookPath_CallsNextWithoutFeatureCheck()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext("/api/orders", "9.9.9.9"));

        called.ShouldBeTrue();
        await _features.DidNotReceiveWithAnyArgs()
            .IsEnabledAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task InvokeAsync_WebhookPathWhenFlagDisabled_CallsNext()
    {
        _features.IsEnabledAsync(FeatureFlags.PaymentCallbackSignatureRequired)
            .Returns(false);
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(BuildContext("/api/payment/callback", "9.9.9.9"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WebhookPathWithAllowedIp_CallsNext()
    {
        _features.IsEnabledAsync(FeatureFlags.PaymentCallbackSignatureRequired)
            .Returns(true);
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            allowedIps: ["10.0.0.1"]);

        await sut.InvokeAsync(BuildContext("/api/payment/callback/verify", "10.0.0.1"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WebhookPathWithDisallowedIp_Returns403()
    {
        _features.IsEnabledAsync(FeatureFlags.PaymentCallbackSignatureRequired)
            .Returns(true);
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            allowedIps: ["10.0.0.1"]);
        var context = BuildContext("/api/payment/callback", "9.9.9.9");

        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_WebhookPathWithNoAllowedIpsConfigured_Returns403()
    {
        _features.IsEnabledAsync(FeatureFlags.PaymentCallbackSignatureRequired)
            .Returns(true);
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            allowedIps: []);

        var context = BuildContext("/api/payment/callback", "10.0.0.1");
        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
    }
}
