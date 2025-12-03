namespace Infrastructure.Payment.ZarinPal;

public class ZarinPalPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly ZarinpalSettingsDto _settings;
    private readonly ILogger<ZarinPalPaymentGateway> _logger;

    public string GatewayName => "ZarinPal";

    public ZarinPalPaymentGateway(HttpClient httpClient, IOptions<ZarinpalSettingsDto> settings, ILogger<ZarinPalPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PaymentRequestResultDto> RequestPaymentAsync(decimal amount, string description, string callbackUrl, string? mobile, string? email)
    {
        var requestUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment/request.json"
            : "https://api.zarinpal.com/pg/v4/payment/request.json";

        var payload = new ZarinpalRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amount,
            Description = description,
            CallbackURL = callbackUrl,
            Metadata = new ZarinpalMetadataDto { Mobile = mobile, Email = email }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("ZarinPal Request Failed. Status: {Status}, Content: {Content}", response.StatusCode, content);
                return new PaymentRequestResultDto { IsSuccess = false, Message = "Gateway connection failed.", RawResponse = content };
            }

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

            _logger.LogWarning("ZarinPal returned error code: {Code}", result?.Data?.Code);
            return new PaymentRequestResultDto { IsSuccess = false, Message = "Payment request denied by gateway.", RawResponse = content };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ZarinPal RequestPayment");
            return new PaymentRequestResultDto { IsSuccess = false, Message = ex.Message };
        }
    }

    public async Task<GatewayVerificationResultDto> VerifyPaymentAsync(string authority, int amount)
    {
        var verifyUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json"
            : "https://api.zarinpal.com/pg/v4/payment/verify.json";

        var payload = new ZarinpalVerificationRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amount,
            Authority = authority
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(verifyUrl, payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal verification failed with status {StatusCode}. Response: {Response}", response.StatusCode, content);
                return new GatewayVerificationResultDto { IsVerified = false, Message = "Gateway connection error", RawResponse = content };
            }

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
                    Message = result.Data.Code == 101 ? "Already verified" : "Verified",
                    RawResponse = content
                };
            }

            return new GatewayVerificationResultDto
            {
                IsVerified = false,
                Message = $"Verification failed. Code: {result?.Data?.Code}",
                RawResponse = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in ZarinPal VerifyPayment");
            return new GatewayVerificationResultDto { IsVerified = false, Message = ex.Message };
        }
    }
}