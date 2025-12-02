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

    public async Task<PaymentRequestResultDto> RequestPaymentAsync(PaymentInitiationDto details)
    {
        var amountInRials = Convert.ToInt64(details.Amount * 10);
        var requestUrl = GetApiUrl(_settings.IsSandbox, "request");

        var requestDto = new ZarinpalRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amountInRials,
            Description = string.IsNullOrWhiteSpace(details.Description) ? "پرداخت سفارش" : details.Description,
            CallbackURL = details.CallbackUrl,
            Metadata = new ZarinpalMetadataDto
            {
                Mobile = details.Mobile,
                Email = details.Email
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestDto);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal request failed. Status: {Status}, Body: {Body}", response.StatusCode, responseContent);
                return new PaymentRequestResultDto { IsSuccess = false, Message = "خطا در ارتباط با درگاه پرداخت." };
            }

            var result = JsonSerializer.Deserialize<ZarinpalRequestResponseDto>(responseContent);

            if (result?.Data != null && (result.Data.Code == 100 || result.Data.Code == 101) && !string.IsNullOrEmpty(result.Data.Authority))
            {
                var gatewayUrl = GetPaymentGatewayUrl(_settings.IsSandbox, result.Data.Authority);
                return new PaymentRequestResultDto
                {
                    IsSuccess = true,
                    Authority = result.Data.Authority,
                    PaymentUrl = gatewayUrl,
                    RedirectUrl = gatewayUrl
                };
            }

            return new PaymentRequestResultDto { IsSuccess = false, Message = "خطا در دریافت شناسه پرداخت از بانک." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ZarinPal RequestPaymentAsync");
            return new PaymentRequestResultDto { IsSuccess = false, Message = "خطای داخلی سیستم در اتصال به بانک." };
        }
    }

    public async Task<GatewayVerificationResultDto> VerifyPaymentAsync(decimal amount, string authority)
    {
        var amountInRials = Convert.ToInt64(amount);
        var requestUrl = GetApiUrl(_settings.IsSandbox, "verify");

        var requestDto = new ZarinpalVerificationRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amountInRials,
            Authority = authority
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestDto);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal verification failed. Status: {Status}, Body: {Body}", response.StatusCode, responseContent);
                return new GatewayVerificationResultDto { IsVerified = false, Message = "تراکنش ناموفق یا لغو شده است." };
            }

            var result = JsonSerializer.Deserialize<ZarinpalVerificationResponseDto>(responseContent);

            if (result?.Data != null && (result.Data.Code == 100 || result.Data.Code == 101))
            {
                return new GatewayVerificationResultDto
                {
                    IsVerified = true,
                    RefId = result.Data.RefID,
                    CardPan = result.Data.CardPan,
                    CardHash = result.Data.CardHash,
                    Fee = result.Data.Fee,
                    Message = "پرداخت با موفقیت انجام شد."
                };
            }

            return new GatewayVerificationResultDto { IsVerified = false, Message = "تراکنش تایید نشد. کد خطا: " + result?.Data?.Code };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ZarinPal VerifyPaymentAsync");
            return new GatewayVerificationResultDto { IsVerified = false, Message = "خطای سیستمی در تایید پرداخت." };
        }
    }

    private string GetApiUrl(bool isSandbox, string endpoint)
    {
        var baseUrl = isSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment"
            : "https://api.zarinpal.com/pg/v4/payment";
        return $"{baseUrl}/{endpoint}.json";
    }

    private string GetPaymentGatewayUrl(bool isSandbox, string authority)
    {
        var baseUrl = isSandbox
            ? "https://sandbox.zarinpal.com/pg/StartPay"
            : "https://www.zarinpal.com/pg/StartPay";
        return $"{baseUrl}/{authority}";
    }
}