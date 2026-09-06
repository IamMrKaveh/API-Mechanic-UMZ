using Infrastructure.Chaos.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Presentation.Common.Middleware;

namespace Tests.Presentation.Common.Middleware;

public class ChaosEngineeringMiddlewareTests
{
    private readonly ILogger<ChaosEngineeringMiddleware> _logger =
        Substitute.For<ILogger<ChaosEngineeringMiddleware>>();

    private ChaosEngineeringMiddleware BuildSut(
        RequestDelegate next,
        ChaosOptions? options = null) =>
        new(next, Options.Create(options ?? new ChaosOptions()), _logger);

    private static DefaultHttpContext BuildContext(string path = "/api/orders")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNext()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions { IsEnabled = false, FaultInjectionRate = 1.0 });
        var context = BuildContext();

        await sut.InvokeAsync(context);

        called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenPathExcluded_CallsNextEvenWithFaultRateOne()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions
            {
                IsEnabled = true,
                FaultInjectionRate = 1.0,
                ExcludedPathPrefixes = ["/health", "/metrics", "/swagger"]
            });

        foreach (var path in new[] { "/health/live", "/metrics", "/swagger/index.html", "/HEALTH/ready" })
        {
            called = false;
            await sut.InvokeAsync(BuildContext(path));
            called.ShouldBeTrue($"path {path} should be excluded");
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenFaultRateIsOne_Returns503WithoutCallingNext()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions
            {
                IsEnabled = true,
                FaultInjectionRate = 1.0,
                LatencyInjectionRate = 0.0,
                ExcludedPathPrefixes = []
            });
        var context = BuildContext("/api/orders");

        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        new StreamReader(context.Response.Body).ReadToEnd().ShouldBe("Chaos injected fault.");
    }

    [Fact]
    public async Task InvokeAsync_WhenFaultRateIsZero_CallsNext()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions
            {
                IsEnabled = true,
                FaultInjectionRate = 0.0,
                LatencyInjectionRate = 0.0,
                ExcludedPathPrefixes = []
            });

        await sut.InvokeAsync(BuildContext());

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenPathNotIncluded_CallsNext()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions
            {
                IsEnabled = true,
                FaultInjectionRate = 1.0,
                IncludedPathPrefixes = ["/api/orders"],
                ExcludedPathPrefixes = []
            });

        await sut.InvokeAsync(BuildContext("/api/other"));

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenPathIncluded_AppliesFault()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions
            {
                IsEnabled = true,
                FaultInjectionRate = 1.0,
                IncludedPathPrefixes = ["/api/orders"],
                ExcludedPathPrefixes = []
            });
        var context = BuildContext("/api/orders/123");

        await sut.InvokeAsync(context);

        called.ShouldBeFalse();
        context.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task InvokeAsync_WhenLatencyRateIsOne_StillCallsNext()
    {
        var called = false;
        var sut = BuildSut(
            _ => { called = true; return Task.CompletedTask; },
            new ChaosOptions
            {
                IsEnabled = true,
                FaultInjectionRate = 0.0,
                LatencyInjectionRate = 1.0,
                MaxLatencyMilliseconds = 5,
                ExcludedPathPrefixes = []
            });

        await sut.InvokeAsync(BuildContext());

        called.ShouldBeTrue();
    }

    [Fact]
    public void UseChaosEngineering_ReturnsBuilder()
    {
        var services = new ServiceCollection();
        var app = new ApplicationBuilder(services.BuildServiceProvider());

        app.UseChaosEngineering().ShouldBeSameAs(app);
    }
}
