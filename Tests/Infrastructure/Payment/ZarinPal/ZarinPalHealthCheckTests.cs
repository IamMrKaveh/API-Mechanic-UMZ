using Infrastructure.Payment.ZarinPal.HealthChecks;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Payment.ZarinPal;

public class ZarinPalHealthCheckTests
{
    private static IHttpClientFactory BuildFactory(FakeHttpMessageHandler handler)
    { var factory = Substitute.For<IHttpClientFactory>(); factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler, disposeHandler: false)); return factory; }

    private static IMemoryCache NewCache()
        => new MemoryCache(Options.Create(new MemoryCacheOptions()));

    private static IOptions<ZarinPalOptions> BuildOptions(
        bool useSandbox = false,
        string apiBaseUrl = "https://api.zarinpal.com/pg/v4/payment/",
        string sandboxApiBaseUrl = "https://sandbox.zarinpal.com/pg/v4/payment/")
    {
        return Options.Create(new ZarinPalOptions
        {
            UseSandbox = useSandbox,
            ApiBaseUrl = apiBaseUrl,
            SandboxApiBaseUrl = sandboxApiBaseUrl
        });
    }

    [Fact]
    public async Task CheckHealthAsync_EndpointReachable_ReturnsHealthy()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "ok");
        var sut = new ZarinPalHealthCheck(BuildFactory(handler), BuildOptions(), NewCache());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_EndpointReturnsErrorStatus_ReturnsDegraded()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.InternalServerError, "err");
        var sut = new ZarinPalHealthCheck(BuildFactory(handler), BuildOptions(), NewCache());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_HttpThrowsException_ReturnsUnhealthy()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new HttpRequestException("boom"));
        var sut = new ZarinPalHealthCheck(BuildFactory(handler), BuildOptions(), NewCache());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_EmptyBaseUrl_ReturnsUnhealthyAndDoesNotCallHttp()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "ok");
        var sut = new ZarinPalHealthCheck(
            BuildFactory(handler),
            BuildOptions(useSandbox: false, apiBaseUrl: ""),
            NewCache());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        handler.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task CheckHealthAsync_CalledTwice_UsesCachedResultOnSecondCall()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "ok");
        var sut = new ZarinPalHealthCheck(BuildFactory(handler), BuildOptions(), NewCache());

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        handler.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUseSandboxTrue_UsesSandboxBaseUrl()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "ok");
        var sut = new ZarinPalHealthCheck(
            BuildFactory(handler),
            BuildOptions(useSandbox: true, sandboxApiBaseUrl: "https://sandbox.zarinpal.com/pg/v4/payment/"),
            NewCache());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        handler.CallCount.ShouldBe(1);
        handler.Requests[0].RequestUri!.ToString().ShouldContain("sandbox.zarinpal.com");
    }
}
