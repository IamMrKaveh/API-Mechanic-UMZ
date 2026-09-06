using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal;
using Infrastructure.Payment.ZarinPal.Options;
using Microsoft.Extensions.Options;
using SharedKernel.Exceptions;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Fakes;

namespace Tests.Infrastructure.Payment.ZarinPal;

public class ZarinPalSandboxGatewayTests
{
    private const string SandboxMerchant = "sandbox-merchant-1";
    private const string LiveMerchant = "live-merchant-1";
    private const string SandboxStartPay = "https://sandbox.zarinpal.com/pg/StartPay/";

    private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private static IOptions<ZarinPalOptions> BuildOptions(
        string? sandboxMerchantId = SandboxMerchant,
        string merchantId = LiveMerchant,
        string sandboxApiBaseUrl = "https://sandbox.zarinpal.com/",
        string sandboxStartPayBaseUrl = SandboxStartPay,
        int timeoutSeconds = 30) =>
        Options.Create(new ZarinPalOptions
        {
            SandboxMerchantId = sandboxMerchantId,
            MerchantId = merchantId,
            SandboxApiBaseUrl = sandboxApiBaseUrl,
            SandboxStartPayBaseUrl = sandboxStartPayBaseUrl,
            TimeoutSeconds = timeoutSeconds
        });

    private static ZarinPalSandboxGateway BuildSut(
        FakeHttpMessageHandler handler,
        IAuditService audit,
        IOptions<ZarinPalOptions>? options = null,
        Uri? presetBaseAddress = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
        {
            var client = new HttpClient(handler, disposeHandler: false);
            if (presetBaseAddress is not null)
                client.BaseAddress = presetBaseAddress;
            return client;
        });
        return new ZarinPalSandboxGateway(options ?? BuildOptions(), factory, audit);
    }

    private static string RequestJson(int code, string? authority) =>
        JsonSerializer.Serialize(new { data = new { code, authority, message = "ok" }, errors = null as object });

    private static string VerifyJson(int code, long refId = 555L, string? cardPan = "6037-****", decimal fee = 200m) =>
        JsonSerializer.Serialize(new { data = new { code, ref_id = refId, card_pan = cardPan, card_hash = (string?)null, fee, message = "ok" }, errors = null as object });

    private static JsonDocument LastRequestBody(FakeHttpMessageHandler handler)
    {
        handler.RequestBodies.Count.ShouldBeGreaterThan(0);
        return JsonDocument.Parse(handler.RequestBodies[^1]);
    }

    [Fact]
    public void GatewayName_IsZarinpalSandbox()
    {
        var sut = BuildSut(FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}"), _audit);

        sut.GatewayName.ShouldBe("ZarinpalSandbox");
    }

    [Fact]
    public async Task InitiateAsync_Success_ReturnsAuthorityAndSandboxPaymentUrl()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "SANDBOX-AUTH-1"));
        var sut = BuildSut(handler, _audit);

        var result = await sut.InitiateAsync(
            OrderId.NewId(), Money.Create(150_000m, "IRR"), "desc", "https://shop.example.com/cb");

        result.Authority.ShouldBe("SANDBOX-AUTH-1");
        result.PaymentUrl.ShouldBe($"{SandboxStartPay.TrimEnd('/')}/SANDBOX-AUTH-1");
        result.TransactionId.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task InitiateAsync_Success_TrimsTrailingSlashFromSandboxStartPay()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit, BuildOptions(sandboxStartPayBaseUrl: "https://sandbox.example/StartPay///"));

        var result = await sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        result.PaymentUrl.ShouldBe("https://sandbox.example/StartPay/AUTH1");
    }

    [Fact]
    public async Task InitiateAsync_WhenDescriptionEmpty_UsesDefaultWalletTopUpDescription()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);
        var orderId = OrderId.NewId();

        await sut.InitiateAsync(orderId, Money.Create(1000m, "IRR"), "", "https://shop.example.com/cb");
        using var emptyDoc = LastRequestBody(handler);
        emptyDoc.RootElement.GetProperty("description").GetString().ShouldBe($"Wallet TopUp {orderId.Value}");

        var handler2 = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut2 = BuildSut(handler2, _audit);
        await sut2.InitiateAsync(orderId, Money.Create(1000m, "IRR"), "   ", "https://shop.example.com/cb");
        using var wsDoc = LastRequestBody(handler2);
        wsDoc.RootElement.GetProperty("description").GetString().ShouldBe($"Wallet TopUp {orderId.Value}");
    }

    [Fact]
    public async Task InitiateAsync_WhenDescriptionProvided_UsesProvidedDescription()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "my desc", "https://shop.example.com/cb");

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("description").GetString().ShouldBe("my desc");
    }

    [Fact]
    public async Task InitiateAsync_WhenEmailAndPhoneNull_UsesSandboxDefaults()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);
        var orderId = OrderId.NewId();

        await sut.InitiateAsync(orderId, Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("metadata").GetProperty("mobile").GetString().ShouldBe("09000000000");
        doc.RootElement.GetProperty("metadata").GetProperty("email").GetString().ShouldBe("test@example.com");
        doc.RootElement.GetProperty("metadata").GetProperty("order_id").GetString().ShouldBe(orderId.Value.ToString());
    }

    [Fact]
    public async Task InitiateAsync_WhenEmailAndPhoneProvided_UsesProvidedValues()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit);

        await sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb",
            Email.Create("user@example.com"), PhoneNumber.Create("09123456789"));

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("metadata").GetProperty("mobile").GetString().ShouldBe("09123456789");
        doc.RootElement.GetProperty("metadata").GetProperty("email").GetString().ShouldBe("user@example.com");
    }

    [Fact]
    public async Task InitiateAsync_PrefersSandboxMerchantId()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit, BuildOptions(sandboxMerchantId: SandboxMerchant, merchantId: LiveMerchant));

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("merchant_id").GetString().ShouldBe(SandboxMerchant);
    }

    [Fact]
    public async Task InitiateAsync_WhenSandboxMerchantIdEmpty_FallsBackToMerchantId()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit, BuildOptions(sandboxMerchantId: "", merchantId: LiveMerchant));

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("merchant_id").GetString().ShouldBe(LiveMerchant);
    }

    [Fact]
    public async Task InitiateAsync_WhenBothMerchantIdsEmpty_UsesDefaultGuid()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit, BuildOptions(sandboxMerchantId: "  ", merchantId: ""));

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("merchant_id").GetString().ShouldBe("00000000-0000-0000-0000-000000000000");
    }

    [Theory]
    [InlineData("IRT", 15000.0, 150000L)]
    [InlineData("TOMAN", 2000.0, 20000L)]
    [InlineData("IRR", 150000.0, 150000L)]
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
    public async Task InitiateAsync_UsesRequestPath()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        var sut = BuildSut(handler, _audit, presetBaseAddress: new Uri("https://sandbox.zarinpal.com/"));

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        handler.Requests[0].RequestUri!.ToString().ShouldContain("pg/v4/payment/request.json");
    }

    [Fact]
    public async Task InitiateAsync_WhenBaseAddressMissing_SetsSandboxBaseAddress()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        HttpClient? captured = null;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
        {
            captured = new HttpClient(handler, disposeHandler: false);
            return captured;
        });
        var sut = new ZarinPalSandboxGateway(BuildOptions(sandboxApiBaseUrl: "https://sandbox.zarinpal.com/pg/v4/payment/"), factory, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        captured.ShouldNotBeNull();
        captured!.BaseAddress!.ToString().ShouldContain("sandbox.zarinpal.com");
    }

    [Fact]
    public async Task InitiateAsync_WhenSandboxApiBaseUrlEmpty_UsesDefaultSandboxUrl()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        HttpClient? captured = null;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
        {
            captured = new HttpClient(handler, disposeHandler: false);
            return captured;
        });
        var sut = new ZarinPalSandboxGateway(BuildOptions(sandboxApiBaseUrl: ""), factory, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        captured!.BaseAddress!.ToString().ShouldBe("https://sandbox.zarinpal.com/");
    }

    [Fact]
    public async Task InitiateAsync_WhenBaseAddressAlreadySet_PreservesIt()
    {
        var preset = new Uri("https://custom.example/pay/");
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        HttpClient? captured = null;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
        {
            captured = new HttpClient(handler, disposeHandler: false) { BaseAddress = preset };
            return captured;
        });
        var sut = new ZarinPalSandboxGateway(BuildOptions(), factory, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        captured!.BaseAddress.ShouldBe(preset);
    }

    [Fact]
    public async Task InitiateAsync_SetsTimeoutFromOptions()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        HttpClient? captured = null;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
        {
            captured = new HttpClient(handler, disposeHandler: false);
            return captured;
        });
        var sut = new ZarinPalSandboxGateway(BuildOptions(timeoutSeconds: 45), factory, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        captured!.Timeout.ShouldBe(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task InitiateAsync_WhenTimeoutNonPositive_UsesDefault30Seconds()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "AUTH1"));
        HttpClient? captured = null;
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ =>
        {
            captured = new HttpClient(handler, disposeHandler: false);
            return captured;
        });
        var sut = new ZarinPalSandboxGateway(BuildOptions(timeoutSeconds: 0), factory, _audit);

        await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");

        captured!.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(100, "عملیات موفق.")]
    [InlineData(101, "تراکنش پیش از این تأیید شده است.")]
    [InlineData(-9, "اطلاعات ارسال‌شده ناقص است.")]
    [InlineData(-10, "IP یا مرچنت کد پذیرنده صحیح نیست.")]
    [InlineData(-11, "مرچنت کد فعال نیست.")]
    [InlineData(-22, "شناسه پرداخت نامعتبر یا منقضی شده است.")]
    [InlineData(-50, "مبلغ ارسالی معتبر نیست.")]
    [InlineData(-51, "پرداخت یافت نشد.")]
    [InlineData(-52, "خطای غیرمنتظره در درگاه.")]
    [InlineData(-53, "شناسه پرداخت با تراکنش مطابقت ندارد.")]
    [InlineData(-54, "درخواست مورد نظر آرشیو شده است.")]
    public async Task InitiateAsync_WhenFailed_MapsErrorCodeToMessage(int code, string expectedMessage)
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(code, code == 100 ? "AUTH1" : null));
        var sut = BuildSut(handler, _audit);

        // code 100 with null authority also fails; others fail by code
        if (code == 100)
        {
            var ok = await sut.InitiateAsync(OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb");
            ok.Authority.ShouldNotBeNullOrWhiteSpace();
            return;
        }

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.ServiceName.ShouldBe("ZarinpalSandbox");
        ex.Message.ShouldBe(expectedMessage);
        ex.ErrorCode.ShouldBe(code.ToString());
        await _audit.Received(1).LogErrorAsync(Arg.Is<string>(s => s.Contains($"code={code}")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateAsync_WhenCodeUnknown_IncludesCodeInMessage()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(-999, null));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.Message.ShouldContain("-999");
        ex.ErrorCode.ShouldBe("-999");
    }

    [Fact]
    public async Task InitiateAsync_WhenBodyNull_ThrowsWithFallbackCode()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.ErrorCode.ShouldBe("-1");
    }

    [Fact]
    public async Task InitiateAsync_WhenAuthorityMissing_Throws()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, RequestJson(100, "   "));
        var sut = BuildSut(handler, _audit);

        await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));
    }

    [Fact]
    public async Task InitiateAsync_WhenHttpThrows_LogsAndWrapsInExternalServiceException()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new HttpRequestException("no connection"));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.InitiateAsync(
            OrderId.NewId(), Money.Create(1000m, "IRR"), "d", "https://shop.example.com/cb"));

        ex.Message.ShouldBe("ارتباط با درگاه پرداخت سندباکس برقرار نشد.");
        ex.InnerException.ShouldBeOfType<HttpRequestException>();
        await _audit.Received(1).LogErrorAsync(Arg.Is<string>(s => s.Contains("Initiate exception")), Arg.Any<CancellationToken>());
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
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyJson(code, 777L, "6037-****-2222", 300m));
        var sut = BuildSut(handler, _audit);

        var result = await sut.VerifyAsync("AUTH1", Money.Create(150_000m, "IRR"));

        result.IsVerified.ShouldBeTrue();
        result.RefId.ShouldBe(777L);
        result.CardPan.ShouldBe("6037-****-2222");
        result.Fee.ShouldBe(300m);
        result.TransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task VerifyAsync_SendsMerchantIdAmountAndAuthority()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyJson(100));
        var sut = BuildSut(handler, _audit, BuildOptions(sandboxMerchantId: SandboxMerchant));

        await sut.VerifyAsync("AUTH-XYZ", Money.Create(2000m, "TOMAN"));

        handler.Requests[0].RequestUri!.ToString().ShouldContain("pg/v4/payment/verify.json");
        using var doc = LastRequestBody(handler);
        doc.RootElement.GetProperty("merchant_id").GetString().ShouldBe(SandboxMerchant);
        doc.RootElement.GetProperty("amount").GetInt64().ShouldBe(20000L);
        doc.RootElement.GetProperty("authority").GetString().ShouldBe("AUTH-XYZ");
    }

    [Fact]
    public async Task VerifyAsync_WhenFailed_ThrowsWithMappedMessage()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, VerifyJson(-51));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR")));

        ex.ServiceName.ShouldBe("ZarinpalSandbox");
        ex.Message.ShouldBe("پرداخت یافت نشد.");
        ex.ErrorCode.ShouldBe("-51");
    }

    [Fact]
    public async Task VerifyAsync_WhenBodyNull_ThrowsWithFallbackCode()
    {
        var handler = FakeHttpMessageHandler.WithResponse(HttpStatusCode.OK, "{}");
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR")));

        ex.ErrorCode.ShouldBe("-1");
        ex.Message.ShouldContain("-1");
    }

    [Fact]
    public async Task VerifyAsync_WhenHttpThrows_LogsAndWrapsInExternalServiceException()
    {
        var handler = FakeHttpMessageHandler.ThrowsException(new HttpRequestException("timeout"));
        var sut = BuildSut(handler, _audit);

        var ex = await Should.ThrowAsync<ExternalServiceException>(() => sut.VerifyAsync("AUTH1", Money.Create(1000m, "IRR")));

        ex.Message.ShouldBe("ارتباط با درگاه پرداخت سندباکس برقرار نشد.");
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
