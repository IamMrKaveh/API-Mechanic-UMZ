using Application.Audit.Contracts;
using Application.Location.Features.Shared;
using Infrastructure.Location.Services;

namespace Tests.Infrastructure.Location.Services;

public class LocationServiceTests
{
    private const string ProvincesErrorMessage = "Failed to fetch provinces from the location API."; private const string CitiesErrorMessage = "Failed to fetch cities for province {StateId} from the location API.";

    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    [Fact]
    public async Task GetProvincesAsync_WhenResponseHasProvinces_ReturnsMappedReadOnlyList()
    {
        const string json = "[{\"id\":1,\"name\":\"Tehran\",\"code\":\"TH\"},{\"id\":2,\"name\":\"Isfahan\",\"code\":\"IS\"}]";
        var handler = new StubHttpMessageHandler((_, _) => JsonOk(json));
        var sut = CreateSut(handler);

        var result = await sut.GetProvincesAsync();

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe(new ProvinceDto(1, "Tehran", "TH"));
        result[1].ShouldBe(new ProvinceDto(2, "Isfahan", "IS"));
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetProvincesAsync_WhenExternalProvinceCodeIsNull_MapsCodeToEmptyString()
    {
        const string json = "[{\"id\":5,\"name\":\"Qom\",\"code\":null}]";
        var handler = new StubHttpMessageHandler((_, _) => JsonOk(json));
        var sut = CreateSut(handler);

        var result = await sut.GetProvincesAsync();

        result.Count.ShouldBe(1);
        result[0].ShouldBe(new ProvinceDto(5, "Qom", string.Empty));
    }

    [Fact]
    public async Task GetProvincesAsync_WhenResponseBodyIsJsonNull_ReturnsEmptyListAndDoesNotLog()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonOk("null"));
        var sut = CreateSut(handler);

        var result = await sut.GetProvincesAsync();

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetProvincesAsync_WhenResponseBodyIsEmptyArray_ReturnsEmptyList()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonOk("[]"));
        var sut = CreateSut(handler);

        var result = await sut.GetProvincesAsync();

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetProvincesAsync_WhenHttpCallFails_LogsProvincesErrorMessageAndRethrows()
    {
        var thrown = new HttpRequestException("network down");
        var handler = new StubHttpMessageHandler((_, _) => throw thrown);
        var sut = CreateSut(handler);

        var actual = await Should.ThrowAsync<HttpRequestException>(() => sut.GetProvincesAsync());

        actual.ShouldBeSameAs(thrown);
        await _auditService.Received(1).LogErrorAsync(ProvincesErrorMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProvincesAsync_IssuesGetRequestAgainstStatesRelativePath()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonOk("[]"));
        var sut = CreateSut(handler);

        _ = await sut.GetProvincesAsync();

        handler.CallCount.ShouldBe(1);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/states");
    }

    [Fact]
    public async Task GetProvincesAsync_PropagatesCancellationTokenToHttpPipelineAndAuditOnFailure()
    {
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            cts.Cancel();
            throw new HttpRequestException("boom");
        });
        var sut = CreateSut(handler);

        await Should.ThrowAsync<HttpRequestException>(() => sut.GetProvincesAsync(ct));

        handler.LastCancellationToken.CanBeCanceled.ShouldBeTrue();
        handler.LastCancellationToken.IsCancellationRequested.ShouldBeTrue();
        await _auditService.Received(1).LogErrorAsync(ProvincesErrorMessage, ct);
    }

    [Fact]
    public async Task GetCitiesByProvinceAsync_WhenResponseHasCities_ReturnsMappedReadOnlyList()
    {
        const string json = "[{\"id\":10,\"name\":\"Karaj\",\"province\":\"Alborz\",\"state_id\":30},{\"id\":11,\"name\":\"Nazarabad\",\"province\":\"Alborz\",\"state_id\":30}]";
        var handler = new StubHttpMessageHandler((_, _) => JsonOk(json));
        var sut = CreateSut(handler);

        var result = await sut.GetCitiesByProvinceAsync("30");

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result[0].ShouldBe(new CityDto(10, "Karaj", "Alborz", 30));
        result[1].ShouldBe(new CityDto(11, "Nazarabad", "Alborz", 30));
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetCitiesByProvinceAsync_WhenExternalProvinceNameIsNull_MapsProvinceNameToEmptyString()
    {
        const string json = "[{\"id\":42,\"name\":\"Anonymous\",\"province\":null,\"state_id\":7}]";
        var handler = new StubHttpMessageHandler((_, _) => JsonOk(json));
        var sut = CreateSut(handler);

        var result = await sut.GetCitiesByProvinceAsync("7");

        result.Count.ShouldBe(1);
        result[0].ShouldBe(new CityDto(42, "Anonymous", string.Empty, 7));
    }

    [Fact]
    public async Task GetCitiesByProvinceAsync_WhenResponseBodyIsJsonNull_ReturnsEmptyListAndDoesNotLog()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonOk("null"));
        var sut = CreateSut(handler);

        var result = await sut.GetCitiesByProvinceAsync("1");

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetCitiesByProvinceAsync_WhenResponseBodyIsEmptyArray_ReturnsEmptyList()
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonOk("[]"));
        var sut = CreateSut(handler);

        var result = await sut.GetCitiesByProvinceAsync("1");

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task GetCitiesByProvinceAsync_WhenHttpCallFails_LogsCitiesErrorMessageAndRethrows()
    {
        var thrown = new HttpRequestException("upstream failure");
        var handler = new StubHttpMessageHandler((_, _) => throw thrown);
        var sut = CreateSut(handler);

        var actual = await Should.ThrowAsync<HttpRequestException>(() => sut.GetCitiesByProvinceAsync("15"));

        actual.ShouldBeSameAs(thrown);
        await _auditService.Received(1).LogErrorAsync(CitiesErrorMessage, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCitiesByProvinceAsync_PropagatesCancellationTokenToHttpPipelineAndAuditOnFailure()
    {
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            cts.Cancel();
            throw new HttpRequestException("boom");
        });
        var sut = CreateSut(handler);

        await Should.ThrowAsync<HttpRequestException>(() => sut.GetCitiesByProvinceAsync("15", ct));

        handler.LastCancellationToken.CanBeCanceled.ShouldBeTrue();
        handler.LastCancellationToken.IsCancellationRequested.ShouldBeTrue();
        await _auditService.Received(1).LogErrorAsync(CitiesErrorMessage, ct);
    }

    [Theory]
    [InlineData("1", "/cities?state_id=1")]
    [InlineData("15", "/cities?state_id=15")]
    [InlineData("TH", "/cities?state_id=TH")]
    public async Task GetCitiesByProvinceAsync_IssuesGetRequestAgainstCitiesRelativePathWithStateIdQuery(string provinceId, string expectedPathAndQuery)
    {
        var handler = new StubHttpMessageHandler((_, _) => JsonOk("[]"));
        var sut = CreateSut(handler);

        _ = await sut.GetCitiesByProvinceAsync(provinceId);

        handler.CallCount.ShouldBe(1);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.RequestUri.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe(expectedPathAndQuery);
    }

    private LocationService CreateSut(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://fake.location.local/"),
            Timeout = Timeout.InfiniteTimeSpan
        };
        return new LocationService(client, _auditService);
    }

    private static Task<HttpResponseMessage> JsonOk(string json) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder = responder;

        public HttpRequestMessage? LastRequest { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            CallCount++;
            return _responder(request, cancellationToken);
        }
    }
}
