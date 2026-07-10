using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.ZarinPal.Options;
using SharedKernel.Exceptions;
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

    public async Task<PaymentInitiationResult> InitiateAsync(
        OrderId orderId,
        Money amount,
        string description,
        string callbackUrl,
        Email? email = null,
        PhoneNumber? phoneNumber = null,
        CancellationToken ct = default)
    {
        var merchantId = ResolveMerchantId();
        var amountInRial = ToRial(amount);

        var body = new ZarinPalRequestDto
        {
            MerchantId = merchantId,
            Amount = amountInRial,
            Description = string.IsNullOrWhiteSpace(description) ? $"Wallet TopUp {orderId.Value}" : description,
            CallbackUrl = callbackUrl,
            Metadata = new ZarinPalMetadataDto
            {
                Mobile = phoneNumber?.Value ?? "09000000000",
                Email = email?.Value ?? "test@example.com",
                OrderId = orderId.Value.ToString()
            }
        };

        ZarinPalResponseDto? parsed;
        try
        {
            var client = CreateClient();
            using var response = await client.PostAsJsonAsync(RequestPath, body, ct);
            parsed = await response.Content.ReadFromJsonAsync<ZarinPalResponseDto>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not ExternalServiceException and not OperationCanceledException)
        {
            await auditService.LogErrorAsync($"[ZarinPalSandbox] Initiate exception: {ex.Message}", ct);
            throw new ExternalServiceException(GatewayName, "ارتباط با درگاه پرداخت سندباکس برقرار نشد.", ex);
        }

        var code = parsed?.Data?.Code ?? -1;
        var authority = parsed?.Data?.Authority;

        if (code == 100 && !string.IsNullOrWhiteSpace(authority))
        {
            var startPay = _options.SandboxStartPayBaseUrl.TrimEnd('/');
            return new PaymentInitiationResult(authority, $"{startPay}/{authority}", Guid.Empty);
        }

        await auditService.LogErrorAsync($"[ZarinPalSandbox] Initiate failed code={code}", ct);
        throw new ExternalServiceException(GatewayName, MapErrorCode(code), code.ToString());
    }

    public async Task<PaymentVerificationResult> VerifyAsync(
        string authority,
        Money expectedAmount,
        CancellationToken ct = default)
    {
        var body = new ZarinPalVerifyRequestDto
        {
            MerchantId = ResolveMerchantId(),
            Amount = ToRial(expectedAmount),
            Authority = authority
        };

        ZarinPalVerifyResponseDto? parsed;
        try
        {
            var client = CreateClient();
            using var response = await client.PostAsJsonAsync(VerifyPath, body, ct);
            parsed = await response.Content.ReadFromJsonAsync<ZarinPalVerifyResponseDto>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is not ExternalServiceException and not OperationCanceledException)
        {
            await auditService.LogErrorAsync($"[ZarinPalSandbox] Verify exception: {ex.Message}", ct);
            throw new ExternalServiceException(GatewayName, "ارتباط با درگاه پرداخت سندباکس برقرار نشد.", ex);
        }

        var code = parsed?.Data?.Code ?? -1;
        if (code is 100 or 101)
        {
            return new PaymentVerificationResult(null, true, parsed!.Data!.RefId, parsed.Data.CardPan, parsed.Data.Fee);
        }

        throw new ExternalServiceException(GatewayName, MapErrorCode(code), code.ToString());
    }

    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        if (client.BaseAddress is null)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.SandboxApiBaseUrl)
                ? "https://sandbox.zarinpal.com/"
                : _options.SandboxApiBaseUrl;
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 30);
        return client;
    }

    private string ResolveMerchantId()
    {
        if (!string.IsNullOrWhiteSpace(_options.SandboxMerchantId))
            return _options.SandboxMerchantId!;
        if (!string.IsNullOrWhiteSpace(_options.MerchantId))
            return _options.MerchantId;
        return "00000000-0000-0000-0000-000000000000";
    }

    private static long ToRial(Money amount)
    {
        var value = amount.Amount;
        if (string.Equals(amount.Currency, "IRT", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(amount.Currency, "TOMAN", StringComparison.OrdinalIgnoreCase))
            value *= 10m;
        return (long)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static string MapErrorCode(int code) => code switch
    {
        100 => "عملیات موفق.",
        101 => "تراکنش پیش از این تأیید شده است.",
        -9 => "اطلاعات ارسال‌شده ناقص است.",
        -10 => "IP یا مرچنت کد پذیرنده صحیح نیست.",
        -11 => "مرچنت کد فعال نیست.",
        -22 => "شناسه پرداخت نامعتبر یا منقضی شده است.",
        -50 => "مبلغ ارسالی معتبر نیست.",
        -51 => "پرداخت یافت نشد.",
        -52 => "خطای غیرمنتظره در درگاه.",
        -53 => "شناسه پرداخت با تراکنش مطابقت ندارد.",
        -54 => "درخواست مورد نظر آرشیو شده است.",
        _ => $"پرداخت تأیید نشد. کد خطا: {code}"
    };
}

internal sealed class ZarinPalRequestDto
{
    [JsonPropertyName("merchant_id")] public string MerchantId { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public long Amount { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("callback_url")] public string CallbackUrl { get; set; } = string.Empty;
    [JsonPropertyName("metadata")] public ZarinPalMetadataDto Metadata { get; set; } = new();
}

internal sealed class ZarinPalMetadataDto
{
    [JsonPropertyName("mobile")] public string Mobile { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("order_id")] public string OrderId { get; set; } = string.Empty;
}

internal sealed class ZarinPalVerifyRequestDto
{
    [JsonPropertyName("merchant_id")] public string MerchantId { get; set; } = string.Empty;
    [JsonPropertyName("amount")] public long Amount { get; set; }
    [JsonPropertyName("authority")] public string Authority { get; set; } = string.Empty;
}

internal sealed class ZarinPalResponseDto
{
    [JsonPropertyName("data")] public ZarinPalResponseData? Data { get; set; }
    [JsonPropertyName("errors")] public object? Errors { get; set; }
}

internal sealed class ZarinPalResponseData
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("authority")] public string? Authority { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

internal sealed class ZarinPalVerifyResponseDto
{
    [JsonPropertyName("data")] public ZarinPalVerifyResponseData? Data { get; set; }
    [JsonPropertyName("errors")] public object? Errors { get; set; }
}

internal sealed class ZarinPalVerifyResponseData
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("ref_id")] public long RefId { get; set; }
    [JsonPropertyName("card_pan")] public string? CardPan { get; set; }
    [JsonPropertyName("card_hash")] public string? CardHash { get; set; }
    [JsonPropertyName("fee")] public decimal Fee { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}