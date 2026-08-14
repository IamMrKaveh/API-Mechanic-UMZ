using Infrastructure.Communication.HealthChecks;
using Infrastructure.Communication.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Communication.HealthChecks;

public class KavenegarHealthCheckTests
{
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>(); private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private KavenegarHealthCheck CreateSut(KavenegarOptions options)
    {
        return new KavenegarHealthCheck(_httpClientFactory, Options.Create(options), _cache);
    }

    private static KavenegarOptions ValidOptions() => new()
    {
        ApiKey = "test-key",
        Sender = "10008663",
        OtpTemplate = "verify"
    };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CheckHealthAsync_WhenApiKeyIsMissingOrWhitespace_ReturnsUnhealthyAndDoesNotCallHttpClientFactory(string apiKey)
    {
        var options = new KavenegarOptions
        {
            ApiKey = apiKey,
            Sender = "10008663",
            OtpTemplate = "verify"
        };
        var sut = CreateSut(options);

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Kavenegar API key is not configured.");
        _httpClientFactory.DidNotReceiveWithAnyArgs().CreateClient(default!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenKavenegarReturnsSuccessStatusCode_ReturnsHealthy()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(handler, disposeHandler: false));
        var sut = CreateSut(ValidOptions());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("Kavenegar reachable.");
        handler.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSuccessful_CallsAccountInfoEndpointWithConfiguredApiKey()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(handler, disposeHandler: false));
        var sut = CreateSut(ValidOptions());

        await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        handler.Requests[0].RequestUri!.ToString()
            .ShouldBe("https://api.kavenegar.com/v1/test-key/account/info.json");
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400)]
    [InlineData(HttpStatusCode.Unauthorized, 401)]
    [InlineData(HttpStatusCode.InternalServerError, 500)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 503)]
    public async Task CheckHealthAsync_WhenKavenegarReturnsNonSuccessStatusCode_ReturnsDegradedWithStatusCodeInDescription(HttpStatusCode statusCode, int expectedCode)
    {
        var handler = FakeHttpMessageHandler.WithResponse(statusCode, "err");
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(handler, disposeHandler: false));
        var sut = CreateSut(ValidOptions());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldBe($"Kavenegar returned HTTP {expectedCode}.");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpClientThrows_ReturnsUnhealthyWithExceptionAttached()
    {
        var exception = new HttpRequestException("network down");
        var handler = FakeHttpMessageHandler.ThrowsException(exception);
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(handler, disposeHandler: false));
        var sut = CreateSut(ValidOptions());

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe("Kavenegar unreachable.");
        result.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCalledTwiceWithinCacheWindow_ExecutesHttpCallOnceAndReturnsSameResult()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(handler, disposeHandler: false));
        var sut = CreateSut(ValidOptions());

        var first = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        var second = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        handler.CallCount.ShouldBe(1);
        first.Status.ShouldBe(HealthStatus.Healthy);
        second.Status.ShouldBe(HealthStatus.Healthy);
        second.Description.ShouldBe(first.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenApiKeyMissingCalledTwice_CachesUnhealthyResultAndDoesNotReevaluate()
    {
        var sut = CreateSut(new KavenegarOptions
        {
            ApiKey = "",
            Sender = "10008663",
            OtpTemplate = "verify"
        });

        var first = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
        var second = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        first.Status.ShouldBe(HealthStatus.Unhealthy);
        second.Status.ShouldBe(HealthStatus.Unhealthy);
        second.Description.ShouldBe(first.Description);
        _httpClientFactory.DidNotReceiveWithAnyArgs().CreateClient(default!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancellationRequested_RethrowsOperationCanceledException()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new OperationCanceledException());
        _httpClientFactory.CreateClient(Arg.Any<string>())
            .Returns(_ => new HttpClient(handler, disposeHandler: false));
        var sut = CreateSut(ValidOptions());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.CheckHealthAsync(new HealthCheckContext(), cts.Token));
    }
}
