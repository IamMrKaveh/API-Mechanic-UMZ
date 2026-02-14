namespace Infrastructure.Payment.ZarinPal;

public class ZarinPalPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly ZarinpalSettingsDto _settings;
    private readonly ILogger<ZarinPalPaymentGateway> _logger;

    public string GatewayName => "ZarinPal";

    public ZarinPalPaymentGateway(
        HttpClient httpClient,
        IOptions<ZarinpalSettingsDto> settings,
        ILogger<ZarinPalPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaymentRequestResultDto> RequestPaymentAsync(
        decimal amount,
        string description,
        string callbackUrl,
        string? mobile = null,
        string? email = null)
    {
        var requestUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment/request.json"
            : "https://api.zarinpal.com/pg/v4/payment/request.json";

        var payload = new
        {
            merchant_id = _settings.MerchantId,
            amount = amount,
            description = description,
            callback_url = callbackUrl,
            metadata = new { mobile, email }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ZarinpalRequestResponseDto>(content);

            if (result?.Data != null && result.Data.Code == 100)
            {
                var paymentUrl = _settings.IsSandbox
                    ? $"https://sandbox.zarinpal.com/pg/StartPay/{result.Data.Authority}"
                    : $"https://www.zarinpal.com/pg/StartPay/{result.Data.Authority}";

                return new PaymentRequestResultDto
                {
                    IsSuccess = true,
                    Authority = result.Data.Authority,
                    PaymentUrl = paymentUrl,
                    RawResponse = content
                };
            }

            var errorMessage = result?.Data != null
                ? ZarinPalErrorMapper.GetMessage(result.Data.Code)
                : "خطا در برقراری ارتباط با درگاه";

            _logger.LogError("ZarinPal Request Failed. Code: {Code}, Content: {Content}",
                result?.Data?.Code, content);

            return new PaymentRequestResultDto
            {
                IsSuccess = false,
                Message = errorMessage,
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ZarinPal RequestPayment");
            return new PaymentRequestResultDto
            {
                IsSuccess = false,
                Message = "خطای داخلی سرور در ارتباط با درگاه پرداخت"
            };
        }
    }

    public async Task<GatewayVerificationResultDto> VerifyPaymentAsync(string authority, int amount)
    {
        var verifyUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json"
            : "https://api.zarinpal.com/pg/v4/payment/verify.json";

        var payload = new
        {
            merchant_id = _settings.MerchantId,
            amount = amount,
            authority = authority
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(verifyUrl, payload);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ZarinpalVerificationResponseDto>(content);

            if (result?.Data != null && (result.Data.Code == 100 || result.Data.Code == 101))
            {
                return new GatewayVerificationResultDto
                {
                    IsVerified = true,
                    RefId = result.Data.RefID,
                    CardPan = result.Data.CardPan,
                    CardHash = result.Data.CardHash,
                    Fee = result.Data.Fee,
                    Message = result.Data.Code == 101
                        ? "تراکنش قبلاً تایید شده است"
                        : "تراکنش با موفقیت تایید شد",
                    RawResponse = content
                };
            }

            return new GatewayVerificationResultDto
            {
                IsVerified = false,
                Message = ZarinPalErrorMapper.GetMessage(result?.Data?.Code ?? -52),
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ZarinPal VerifyPayment for authority {Authority}", authority);
            return new GatewayVerificationResultDto
            {
                IsVerified = false,
                Message = "خطای داخلی در تایید پرداخت"
            };
        }
    }
}