using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.Extensions.Options;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Payment.ZarinPal;

public class ZarinPalPaymentGatewayTests
{
    private const string MerchantId = "merchant-id-123";
    private const string StartPayBase = "https://www.zarinpal.com/pg/StartPay/";

    private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private static IOptions<ZarinPalOptions> BuildOptions(string startPayBaseUrl = StartPayBase) =>
        Options.Create(new ZarinPalOptions
        {
            MerchantId = MerchantId,
            StartPayBaseUrl = startPayBaseUrl,
            ApiBaseUrl = "https://api.zarinpal.com/pg/v4/payment/"
        });

    private static ZarinPalPaymentGateway BuildSut(
        FakeHttpMessageHandler handler,
        IAuditService audit,
        IOptions<ZarinPalOptions>? options = null)
    {
        var client = new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://api.zarinpal.com/pg/v4/payment/")
        };
        return new ZarinPalPaymentGateway(client, options ?? BuildOptions(), audit);
    }

    private static string RequestJson(int code, string? authority) =>
        JsonSerializer.Serialize(new { data = new { code, authority, message = "ok" } });

    private static string RequestErrorJson(int code) =>
        JsonSerializer.Serialize(new { data = null as object, errors = new { code, message = "err" } });

    private static string VerifyJson(int code, long refId = 123456L, string? cardPan = "6037-****", decimal fee = 1000m) =>
        JsonSerializer.Serialize(new { data = new { Code = code, RefId = refId, CardPan = cardPan, Fee = fee, Message = "ok" } });

    private static string VerifyErrorJson(int code) =>
        JsonSerializer.Serialize(new { data = null as object, errors = new { code, message = "err" } });

    private static JsonDocument LastRequestBody(FakeHttpMessageHandler handler)
    {
        handler.RequestBodies.Count.ShouldBeGreaterThan(0);
        return JsonDocument.Parse(handler.RequestBodies[^1]);
    }

    [Fact]
    public void GatewayName_IsZarinpal()
    {
        var sut = BuildSut(FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}"), _audit);

        sut.GatewayName.ShouldBe("Zarinpal");
    }

    [Fact]
    public async Task InitiateAsync_Success_ReturnsAuthorityAndPaymentUrl()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "A000000000000000000000000001"));
        var sut = BuildSut(handler, _audit);

        var result = await sut.InitiateAsync(
            OrderId.NewId(), Money.Create(150_000m, "IRR"), "desc", "https://shop.example.com/cb");

        result.Authority.ShouldBe("A000000000000000000000000001");
        result.PaymentUrl.ShouldBe($"{StartPayBase.TrimEnd('/')}/A000000000000000000000000001");
        result.TransactionId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task InitiateAsync_Success_TrimsTrailingSlashFromStartPayBaseUrl()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH123"));
        var sut = BuildSut(handler, _audit, BuildOptions("https://pay.example/StartPay///"));

        var result = await sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "desc", "https://shop.example.com/cb");

        result.PaymentUrl.ShouldBe("https://pay.example/StartPay/AUTH123");
    }

    [Fact]
    public async Task InitiateAsync_SendsMerchantIdAmountAndMetadata()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);
        var orderId = OrderId.NewId();
        var email = Email.Create("user@example.com");
        var phone = PhoneNumber.Create("09123456789");

        await sut.InitiateAsync(orderId, Money.Create(150_000m, "IRR"), "desc", "https://shop.example.com/cb", email, phone);

        using var doc = LastRequestBody(handler);
        var root = doc.RootElement;
        root.GetProperty("merchant_id").GetString().ShouldBe(MerchantId);
        root.GetProperty("amount").GetInt64().ShouldBe(150_000L);
        root.GetProperty("description").GetString().ShouldBe("desc");
        root.GetProperty("callback_url").GetString().ShouldBe("https://shop.example.com/cb");
        root.GetProperty("metadata").GetProperty("mobile").GetString().ShouldBe("09123456789");
        root.GetProperty("metadata").GetProperty("email").GetString().ShouldBe("user@example.com");
        root.GetProperty("metadata").GetProperty("order_id").GetString().ShouldBe(orderId.Value.ToString());
    }

    [Theory]
    [InlineData("IRT", 15000.0, 150000L)]
    [InlineData("TOMAN", 15000.0, 150000L)]
    [InlineData("toman", 1000.0, 10000L)]
    [InlineData("IRR", 150000.0, 150000L)]
    [InlineData("USD", 99.5, 100L)] // AwayFromZero rounding
    [InlineData("IRR", 1000.4, 1000L)]
    [InlineData("IRR", 1000.5, 1001L)]
    public async Task InitiateAsync_ToRial_ConvertsCurrencyCorrectly(string currency, double amount, long expectedRial)
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create((decimal)amount, currency), "d", "https://shop.example.com/cb");

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("amount").GetInt64().ShouldBe(expectedRial);
    }

    [Fact]
    public async Task InitiateAsync_WhenEmailAndPhoneNull_OmitsMetadataValuesAsNull()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        handler.Requests[0].RequestUri!.ToString().ShouldContain("request.json");
        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("metadata").GetProperty("order_id").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(-11, "مرچنت کد نامعتبر است.")]
    [InlineData(-22, "شناسه پرداخت نامعتبر یا منقضی شده است.")]
    [InlineData(-50, "مبلغ پرداخت معتبر نیست.")]
    public async Task InitiateAsync_WhenGatewayReturnsError_ThrowsWithMappedMessage(int code, string expectedMessage)
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestErrorJson(code));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.ServiceName.ShouldBe("Zarinpal");
        ex.Message.ShouldBe(expectedMessage);
        ex.ErrorCode.ShouldBe(code.ToString());
        await _audit.Received(1).LogErrorAsync(Arg.Is<string>(s => s.Contains($"code={code}")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateAsync_WhenBodyIsNull_ThrowsWithFallbackCode()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.ErrorCode.ShouldBe("-1");
    }

    [Theory]
    [InlineData(200, "AUTH1")]
    [InlineData(100, null)]
    [InlineData(100, "")]
    [InlineData(100, "   ")]
    public async Task InitiateAsync_WhenCodeNot100OrAuthorityMissing_Throws(int code, string? authority)
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(code, authority));
        var sut = BuildSut(handler, _audit);

        await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));
    }

    [Fact]
    public async Task InitiateAsync_WhenHttpThrows_LogsAndWrapsInExternalServiceException()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new HttpRequestException("network down"));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.ServiceName.ShouldBe("Zarinpal");
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        await _audit.Received(1).LogErrorAsync(Arg.Is<string>(s => s.Contains("Initiate exception")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateAsync_WhenResponseIsInvalidJson_ThrowsExternalServiceException()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "not-json{{{");
        var sut = BuildSut(handler, _audit);

        await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));
    }

    [Fact]
    public async Task InitiateAsync_WhenCancelled_PropagatesCancellation()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb", null, null, cts.Token));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    public async Task VerifyAsync_SuccessCodes_ReturnVerifiedResult(int code)
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyJson(code, 987654L, "6037-****-1111", 1500m));
        var sut = BuildSut(handler, _audit);

        var result = await sut.VerifyAsync("AUTH1", Money.Create(150_000m, "IRR"));

        result.IsVerified.ShouldBeTrue();
        result.RefId.ShouldBe(987654L);
        result.CardPan.ShouldBe("6037-****-1111");
        result.Fee.ShouldBe(1500m);
        result.TransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task VerifyAsync_SendsMerchantIdAmountAndAuthority()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyJson(100));
        var sut = BuildSut(handler, _audit);

        await sut.VerifyAsync("AUTH-XYZ", Money.Create(15000m, "IRT"));

        handler.Requests[0].RequestUri!.ToString().ShouldContain("verify.json");
        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("merchant_id").GetString().ShouldBe(MerchantId);
        doc.RootElement.GetProperty("amount").GetInt64().ShouldBe(150000L); // IRT toman -> rial
        doc.RootElement.GetProperty("authority").GetString().ShouldBe("AUTH-XYZ");
    }

    [Fact]
    public async Task VerifyAsync_WhenFailed_ThrowsWithMappedMessageAndCode()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyErrorJson(-51));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR")));

        ex.ServiceName.ShouldBe("Zarinpal");
        ex.Message.ShouldBe("پرداخت یافت نشد.");
        ex.ErrorCode.ShouldBe("-51");
    }

    [Fact]
    public async Task VerifyAsync_WhenBodyIsNull_ThrowsWithFallbackCode()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR")));

        ex.ErrorCode.ShouldBe("-1");
    }

    [Fact]
    public async Task VerifyAsync_WhenHttpThrows_LogsAndWrapsInExternalServiceException()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new HttpRequestException("timeout"));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR")));

        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        await _audit.Received(1).LogErrorAsync(Arg.Is<string>(s => s.Contains("Verify exception")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyAsync_WhenCancelled_PropagatesCancellation()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyJson(100));
        var sut = BuildSut(handler, _audit);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR"), cts.Token));
    }
}
