using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;
using System.Text.Json.Serialization;

namespace Infrastructure.Payment.ZarinPal;

public sealed class ZarinPalSandboxGateway(
    IOptions<ZarinPalOptions> options,
    IHttpClientFactory httpClientFactory,
    IAuditService auditService) : IPaymentGateway
{
    private const string HttpClientName = "ZarinPalSandbox";
    private const string RequestPath = "pg/v4/payment/request.json";
    private const string VerifyPath = "pg/v4/payment/verify.json";

    private readonly ZarinPalOptions _options = options.Value;

    public string GatewayName => "ZarinpalSandbox";

    public async Task<ServiceResult<PaymentInitiationResult>> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default)
    {
        var merchantId = ResolveMerchantId();
        var amountInRial = ConvertToRial(amount);

        var requestBody = new ZarinPalRequestDto
        {
            MerchantId = merchantId,
            Amount = amountInRial,
            Description = string.IsNullOrWhiteSpace(description) ? $"Order {orderId.Value}" : description,
            CallbackUrl = callbackUrl,
            Metadata = new ZarinPalMetadataDto
            {
                Mobile = phoneNumber?.Value ?? "09000000000",
                Email = email?.Value ?? "test@gmail.com",
                OrderId = orderId.Value.ToString(),
            },
        };

        var client = CreateHttpClient();

        ZarinPalResponseDto? parsed;

        try
        {
            using var response = await client.PostAsJsonAsync(RequestPath, requestBody, ct);
            parsed = await response.Content.ReadFromJsonAsync<ZarinPalResponseDto>(cancellationToken: ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            await auditService.LogErrorAsync(
                $"[ZarinPalSandbox] Timeout calling request endpoint for Order {orderId.Value}: {ex.Message}",
                ct);
            return ServiceResult<PaymentInitiationResult>.Failure(
                "ارتباط با درگاه پرداخت برقرار نشد. لطفاً مجدداً تلاش کنید.");
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"[ZarinPalSandbox] HTTP failure for Order {orderId.Value}: {ex.Message}",
                ct);
            return ServiceResult<PaymentInitiationResult>.Failure(
                "ارتباط با درگاه پرداخت برقرار نشد.");
        }

        var code = parsed?.Data?.Code ?? -1;
        var authority = parsed?.Data?.Authority;

        if (code != 100 || string.IsNullOrWhiteSpace(authority))
        {
            var errorMessage = parsed?.Errors?.Message ?? $"ZarinPal code {code}";
            await auditService.LogErrorAsync(
                $"[ZarinPalSandbox] Initiate failed for Order {orderId.Value} code {code}: {errorMessage}",
                ct);
            return ServiceResult<PaymentInitiationResult>.Failure(
                "ایجاد تراکنش در درگاه پرداخت ناموفق بود.");
        }

        var startPayBase = ResolveStartPayBaseUrl().TrimEnd('/');
        var paymentUrl = $"{startPayBase}/{authority}";

        await auditService.LogInformationAsync(
            $"[ZarinPalSandbox] Initiated for Order {orderId.Value} amount {amountInRial} authority {authority}",
            ct);

        return ServiceResult<PaymentInitiationResult>.Success(
            new PaymentInitiationResult(authority, paymentUrl));
    }

    public async Task<ServiceResult<PaymentVerificationResult>> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(authority))
            return ServiceResult<PaymentVerificationResult>.Failure("شناسه تراکنش معتبر نیست.");

        var merchantId = ResolveMerchantId();
        var amountInRial = ConvertToRial(expectedAmount);

        var requestBody = new ZarinPalVerifyRequestDto
        {
            MerchantId = merchantId,
            Amount = amountInRial,
            Authority = authority,
        };

        var client = CreateHttpClient();

        ZarinPalVerifyResponseDto? parsed;

        try
        {
            using var response = await client.PostAsJsonAsync(VerifyPath, requestBody, ct);
            parsed = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponseDto>(cancellationToken: ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            await auditService.LogErrorAsync(
                $"[ZarinPalSandbox] Verify timeout for authority {authority}: {ex.Message}",
                ct);
            return ServiceResult<PaymentVerificationResult>.Failure(
                "ارتباط با درگاه پرداخت برقرار نشد. لطفاً مجدداً تلاش کنید.");
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"[ZarinPalSandbox] Verify HTTP failure for authority {authority}: {ex.Message}",
                ct);
            return ServiceResult<PaymentVerificationResult>.Failure(
                "ارتباط با درگاه پرداخت برقرار نشد.");
        }

        var code = parsed?.Data?.Code ?? -1;
        var refId = parsed?.Data?.RefId ?? 0;
        var cardPan = parsed?.Data?.CardPan;
        var feeAmount = parsed?.Data?.Fee ?? 0m;

        if (code == 100 || code == 101)
        {
            await auditService.LogInformationAsync(
                $"[ZarinPalSandbox] Verify success for authority {authority} refId {refId} code {code}",
                ct);
            return ServiceResult<PaymentVerificationResult>.Success(
                new PaymentVerificationResult(null, true, refId, cardPan, feeAmount));
        }

        var errorMessage = parsed?.Errors?.Message
                           ?? MapZarinPalErrorCode(code);

        await auditService.LogWarningAsync(
            $"[ZarinPalSandbox] Verify rejected for authority {authority} code {code}: {errorMessage}",
            ct);

        return ServiceResult<PaymentVerificationResult>.Failure(errorMessage);
    }

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
        {
            var baseUrl = ResolveApiBaseUrl();
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }
        if (client.Timeout == TimeSpan.FromSeconds(100))
        {
            var timeout = _options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 30;
            client.Timeout = TimeSpan.FromSeconds(timeout);
        }
        return client;
    }

    private string ResolveMerchantId()
    {
        var merchantId = _options.SandboxMerchantId;
        if (string.IsNullOrWhiteSpace(merchantId))
            merchantId = _options.MerchantId;
        return string.IsNullOrWhiteSpace(merchantId)
            ? "00000000-0000-0000-0000-000000000000"
            : merchantId;
    }

    private string ResolveApiBaseUrl()
    {
        var url = _options.SandboxApiBaseUrl;
        if (string.IsNullOrWhiteSpace(url))
            url = "https://sandbox.zarinpal.com/";
        return url.EndsWith('/') ? url : url + "/";
    }

    private string ResolveStartPayBaseUrl()
    {
        var url = _options.SandboxStartPayBaseUrl;
        return string.IsNullOrWhiteSpace(url)
            ? "https://sandbox.zarinpal.com/pg/StartPay"
            : url!;
    }

    private static long ConvertToRial(Money amount)
    {
        var currency = amount.Currency?.ToUpperInvariant();
        var value = amount.Amount;
        if (currency == "IRT" || currency == "TOMAN")
            value *= 10m;
        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static string MapZarinPalErrorCode(int code) => code switch
    {
        -9 => "اطلاعات ارسال شده ناقص است.",
        -10 => "آی‌پی یا مرچنت کد پذیرنده صحیح نیست.",
        -11 => "مرچنت کد فعال نیست.",
        -12 => "تلاش بیش از حد در یک بازه زمانی کوتاه.",
        -15 => "تجار شما به دلیل عدم پرداخت کارمزد غیر فعال شده است.",
        -16 => "سطح تایید پذیرنده پایین‌تر از سطح نقره‌ای است.",
        -30 => "اجازه دسترسی به تسویه اشتراکی شناور ندارید.",
        -31 => "حساب بانکی تسویه را به پنل اضافه کنید.",
        -32 => "مبلغ ارسالی از حد مجاز کمتر است.",
        -33 => "درصد‌های وارد شده با مبلغ همخوانی ندارد.",
        -34 => "مبلغ بیشتر از حد مجاز ارسال است.",
        -35 => "تعداد دریافت‌کنندگان تسویه بیشتر از حد مجاز است.",
        -40 => "اجازه دسترسی به متد مربوطه وجود ندارد.",
        -41 => "اطلاعات ارسال شده نامعتبر است.",
        -42 => "مدت زمان معتبر طول عمر شناسه پرداخت بین ۳۰ دقیقه تا ۴۵ روز است.",
        -50 => "مبلغ پرداخت شده با مقدار ارسال شده در متد verify متفاوت است.",
        -51 => "پرداخت ناموفق.",
        -52 => "خطای غیر منتظره. در صورت بروز این خطا با پشتیبانی تماس بگیرید.",
        -53 => "اتوریتی برای این مرچنت کد نیست.",
        -54 => "اتوریتی نامعتبر است.",
        101 => "تراکنش پیش از این تأیید شده است.",
        _ => $"پرداخت تأیید نشد. کد خطا: {code}",
    };

    private sealed class ZarinPalRequestDto
    {
        [JsonPropertyName("merchant_id")] public string MerchantId { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public long Amount { get; set; }
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("callback_url")] public string CallbackUrl { get; set; } = string.Empty;
        [JsonPropertyName("metadata")] public ZarinPalMetadataDto? Metadata { get; set; }
    }

    private sealed class ZarinPalMetadataDto
    {
        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
        [JsonPropertyName("mobile")] public string Mobile { get; set; } = string.Empty;
        [JsonPropertyName("order_id")] public string? OrderId { get; set; }
    }

    private sealed class ZarinPalResponseDto
    {
        [JsonPropertyName("data")] public ZarinPalResponseDataDto? Data { get; set; }
        [JsonPropertyName("errors")] public ZarinPalErrorsDto? Errors { get; set; }
    }

    private sealed class ZarinPalResponseDataDto
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("authority")] public string? Authority { get; set; }
        [JsonPropertyName("fee_type")] public string? FeeType { get; set; }
        [JsonPropertyName("fee")] public decimal Fee { get; set; }
    }

    private sealed class ZarinPalVerifyRequestDto
    {
        [JsonPropertyName("merchant_id")] public string MerchantId { get; set; } = string.Empty;
        [JsonPropertyName("amount")] public long Amount { get; set; }
        [JsonPropertyName("authority")] public string Authority { get; set; } = string.Empty;
    }

    private sealed class ZarinPalVerifyResponseDto
    {
        [JsonPropertyName("data")] public ZarinPalVerifyDataDto? Data { get; set; }
        [JsonPropertyName("errors")] public ZarinPalErrorsDto? Errors { get; set; }
    }

    private sealed class ZarinPalVerifyDataDto
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("card_hash")] public string? CardHash { get; set; }
        [JsonPropertyName("card_pan")] public string? CardPan { get; set; }
        [JsonPropertyName("ref_id")] public long RefId { get; set; }
        [JsonPropertyName("fee_type")] public string? FeeType { get; set; }
        [JsonPropertyName("fee")] public decimal Fee { get; set; }
    }

    [JsonConverter(typeof(ZarinPalErrorsDtoConverter))]
    private sealed class ZarinPalErrorsDto
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    private sealed class ZarinPalErrorsDtoConverter : JsonConverter<ZarinPalErrorsDto>
    {
        public override ZarinPalErrorsDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDocument = JsonDocument.ParseValue(ref reader);
            var root = jsonDocument.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var dto = new ZarinPalErrorsDto();
            if (root.TryGetProperty("code", out var codeElement) && codeElement.ValueKind == JsonValueKind.Number)
                dto.Code = codeElement.GetInt32();
            if (root.TryGetProperty("message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                dto.Message = messageElement.GetString();
            return dto;
        }

        public override void Write(Utf8JsonWriter writer, ZarinPalErrorsDto value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("code", value.Code);
            writer.WriteString("message", value.Message);
            writer.WriteEndObject();
        }
    }
}