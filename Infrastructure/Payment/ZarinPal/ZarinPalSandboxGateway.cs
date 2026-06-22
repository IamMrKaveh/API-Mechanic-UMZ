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
                Email = email?.Value,
                Mobile = phoneNumber?.Value,
                OrderId = orderId.Value.ToString(),
            },
        };

        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
        {
            client.BaseAddress = new Uri(ResolveApiBaseUrl(), UriKind.Absolute);
        }

        ZarinPalResponseDto? parsed = null;

        try
        {
            var response = await client.PostAsJsonAsync(RequestPath, requestBody, ct);
            parsed = await response.Content.ReadFromJsonAsync<ZarinPalResponseDto>(cancellationToken: ct);
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
        {
            return ServiceResult<PaymentVerificationResult>.Failure("شناسه تراکنش معتبر نیست.");
        }

        var merchantId = ResolveMerchantId();
        var amountInRial = ConvertToRial(expectedAmount);

        var requestBody = new ZarinPalVerifyRequestDto
        {
            MerchantId = merchantId,
            Amount = amountInRial,
            Authority = authority,
        };

        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
        {
            client.BaseAddress = new Uri(ResolveApiBaseUrl(), UriKind.Absolute);
        }

        ZarinPalVerifyResponseDto? parsed = null;

        try
        {
            var response = await client.PostAsJsonAsync(VerifyPath, requestBody, ct);
            parsed = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponseDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync(
                $"[ZarinPalSandbox] Verify HTTP failure for authority {authority}: {ex.Message}",
                ct);
            return ServiceResult<PaymentVerificationResult>.Failure("ارتباط با درگاه پرداخت برقرار نشد.");
        }

        var code = parsed?.Data?.Code ?? -1;
        var refId = parsed?.Data?.RefId ?? 0;
        var cardPan = parsed?.Data?.CardPan;
        var feeAmount = parsed?.Data?.Fee ?? 0m;

        var isSuccessful = code == 100 || code == 101;

        if (!isSuccessful)
        {
            var errorMessage = parsed?.Errors?.Message ?? $"ZarinPal code {code}";
            await auditService.LogWarningAsync(
                $"[ZarinPalSandbox] Verify failed for authority {authority} code {code}: {errorMessage}",
                ct);
            return ServiceResult<PaymentVerificationResult>.Success(
                new PaymentVerificationResult(null, false, 0, null, 0m));
        }

        await auditService.LogInformationAsync(
            $"[ZarinPalSandbox] Verify success for authority {authority} refId {refId}",
            ct);

        return ServiceResult<PaymentVerificationResult>.Success(
            new PaymentVerificationResult(null, true, refId, cardPan, feeAmount));
    }

    private string ResolveMerchantId()
    {
        var merchantId = _options.SandboxMerchantId;
        if (string.IsNullOrWhiteSpace(merchantId))
        {
            merchantId = _options.MerchantId;
        }

        return string.IsNullOrWhiteSpace(merchantId)
            ? "00000000-0000-0000-0000-000000000000"
            : merchantId;
    }

    private string ResolveApiBaseUrl()
    {
        var url = _options.SandboxApiBaseUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            url = "https://sandbox.zarinpal.com/";
        }

        return url.EndsWith('/') ? url : url + "/";
    }

    private string ResolveStartPayBaseUrl()
    {
        var url = _options.SandboxStartPayBaseUrl;
        return string.IsNullOrWhiteSpace(url)
            ? "https://sandbox.zarinpal.com/pg/StartPay"
            : url;
    }

    private static long ConvertToRial(Money amount)
    {
        var currency = amount.Currency?.ToUpperInvariant();
        var value = amount.Amount;

        if (currency == "IRT" || currency == "TOMAN")
        {
            value *= 10m;
        }

        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private sealed class ZarinPalRequestDto
    {
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("callback_url")]
        public string CallbackUrl { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public ZarinPalMetadataDto? Metadata { get; set; }
    }

    private sealed class ZarinPalMetadataDto
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("mobile")]
        public string? Mobile { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }
    }

    private sealed class ZarinPalResponseDto
    {
        [JsonPropertyName("data")]
        public ZarinPalResponseDataDto? Data { get; set; }

        [JsonPropertyName("errors")]
        public ZarinPalErrorsDto? Errors { get; set; }
    }

    private sealed class ZarinPalResponseDataDto
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("authority")]
        public string? Authority { get; set; }

        [JsonPropertyName("fee_type")]
        public string? FeeType { get; set; }

        [JsonPropertyName("fee")]
        public decimal Fee { get; set; }
    }

    private sealed class ZarinPalVerifyRequestDto
    {
        [JsonPropertyName("merchant_id")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("authority")]
        public string Authority { get; set; } = string.Empty;
    }

    private sealed class ZarinPalVerifyResponseDto
    {
        [JsonPropertyName("data")]
        public ZarinPalVerifyDataDto? Data { get; set; }

        [JsonPropertyName("errors")]
        public ZarinPalErrorsDto? Errors { get; set; }
    }

    private sealed class ZarinPalVerifyDataDto
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("card_hash")]
        public string? CardHash { get; set; }

        [JsonPropertyName("card_pan")]
        public string? CardPan { get; set; }

        [JsonPropertyName("ref_id")]
        public long RefId { get; set; }

        [JsonPropertyName("fee_type")]
        public string? FeeType { get; set; }

        [JsonPropertyName("fee")]
        public decimal Fee { get; set; }
    }

    private sealed class ZarinPalErrorsDto
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}