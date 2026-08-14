using Application.Audit.Contracts;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Communication.Options;
using Infrastructure.Communication.Services;
using Microsoft.Extensions.Options;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Communication.Services;

public class SmsServiceTests
{
    private const string ApiKey = "test-api-key"; private const string OtpTemplate = "verify"; private const string ExpectedLookupUrl = "https://api.kavenegar.com/v1/test-api-key/verify/lookup.json";

    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private readonly KavenegarOptions _options = new()
    {
        ApiKey = ApiKey,
        Sender = "10008663",
        OtpTemplate = OtpTemplate
    };

    private SmsService CreateSut(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new SmsService(httpClient, Options.Create(_options), _auditService);
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenKavenegarReturns200AndStatus200_ReturnsTrue()
    {
        var handler = FakeHttpMessageHandler.WithResponse(
            HttpStatusCode.OK,
            "{\"return\":{\"status\":200,\"message\":\"OK\"},\"entries\":[]}");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenSuccessful_DoesNotLogError()
    {
        var handler = FakeHttpMessageHandler.WithResponse(
            HttpStatusCode.OK,
            "{\"return\":{\"status\":200,\"message\":\"OK\"}}");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        await sut.SendOtpSMSAsync(phone, code);

        await _auditService.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenSuccessful_PostsFormUrlEncodedRequestToLookupEndpoint()
    {
        var handler = FakeHttpMessageHandler.WithResponse(
            HttpStatusCode.OK,
            "{\"return\":{\"status\":200,\"message\":\"OK\"}}");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        await sut.SendOtpSMSAsync(phone, code);

        handler.CallCount.ShouldBe(1);
        var request = handler.Requests[0];
        request.Method.ShouldBe(HttpMethod.Post);
        request.RequestUri!.ToString().ShouldBe(ExpectedLookupUrl);
        request.Content.ShouldNotBeNull();
        var body = handler.RequestBodies[0];
        body.ShouldContain("receptor=09121234567");
        body.ShouldContain("token=123456");
        body.ShouldContain("template=verify");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SendOtpSMSAsync_WhenKavenegarReturnsNonSuccessStatusCode_ReturnsFalse(HttpStatusCode statusCode)
    {
        var handler = FakeHttpMessageHandler.WithResponse(statusCode, "any body");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenKavenegarReturnsNonSuccessStatusCode_LogsErrorWithStatusCodeAndMaskedPhone()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.InternalServerError, "boom");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        await sut.SendOtpSMSAsync(phone, code);

        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s =>
                s!.Contains("[SMS]") &&
                s.Contains("500") &&
                s.Contains("09*******67") &&
                !s.Contains("09121234567")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenResponseBodyIsMissingReturnProperty_ReturnsFalseAndLogsError()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{\"unexpected\":true}");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s =>
                s!.Contains("[SMS]") &&
                s.Contains("Invalid Kavenegar response") &&
                s.Contains("09*******67")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenKavenegarStatusIsNot200_ReturnsFalseAndLogsErrorWithApiMessage()
    {
        var handler = FakeHttpMessageHandler.WithResponse(
            HttpStatusCode.OK,
            "{\"return\":{\"status\":418,\"message\":\"invalid receptor\"}}");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s =>
                s!.Contains("[SMS]") &&
                s.Contains("Kavenegar API error 418") &&
                s.Contains("invalid receptor") &&
                s.Contains("09*******67")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenResponseBodyIsInvalidJson_ReturnsFalseAndLogsError()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "not-json-at-all");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s =>
                s!.Contains("[SMS]") &&
                s.Contains("Failed to send OTP") &&
                s.Contains("09*******67")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenHttpClientThrows_ReturnsFalseAndLogsError()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new HttpRequestException("network down"));
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s =>
                s!.Contains("[SMS]") &&
                s.Contains("Failed to send OTP") &&
                s.Contains("HttpRequestException") &&
                s.Contains("network down") &&
                s.Contains("09*******67")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendOtpSMSAsync_WhenKavenegarStatusIsNot200AndMessageIsMissing_ReturnsFalseAndLogsErrorFromOuterCatch()
    {
        var handler = FakeHttpMessageHandler.WithResponse(
            HttpStatusCode.OK,
            "{\"return\":{\"status\":401}}");
        var sut = CreateSut(handler);
        var phone = PhoneNumber.Create("09121234567");
        var code = OtpCode.Create("123456");

        var result = await sut.SendOtpSMSAsync(phone, code);

        result.ShouldBeFalse();
        await _auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s =>
                s!.Contains("[SMS]") &&
                s.Contains("Failed to send OTP") &&
                s.Contains("09*******67")),
            Arg.Any<CancellationToken>());
    }
}
