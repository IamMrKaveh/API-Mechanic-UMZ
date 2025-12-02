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

    public async Task<PaymentRequestResultDto> RequestPaymentAsync(PaymentInitiationDto initiationDto)
    {
        var requestUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment/request.json"
            : "https://payment.zarinpal.com/pg/v4/payment/request.json";

        var requestPayload = new ZarinpalRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = initiationDto.Amount,
            Description = initiationDto.Description,
            CallbackURL = initiationDto.CallbackUrl,
            Metadata = new ZarinpalMetadataDto
            {
                Mobile = initiationDto.Mobile,
                Email = initiationDto.Email
            }
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(requestUrl, requestPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ZarinPal Request Failed with Status {StatusCode}. Content: {Content}", response.StatusCode, errorContent);
                return new PaymentRequestResultDto { IsSuccess = false, Message = "خطا در ارتباط با درگاه پرداخت" };
            }

            var responseContent = await response.Content.ReadFromJsonAsync<ZarinpalRequestResponseDto>();

            if (responseContent?.Data != null && (responseContent.Data.Code == 100 || responseContent.Data.Code == 101))
            {
                var paymentUrl = _settings.IsSandbox
                    ? $"https://sandbox.zarinpal.com/pg/StartPay/{responseContent.Data.Authority}"
                    : $"https://payment.zarinpal.com/pg/StartPay/{responseContent.Data.Authority}";

                return new PaymentRequestResultDto
                {
                    IsSuccess = true,
                    Authority = responseContent.Data.Authority,
                    PaymentUrl = paymentUrl
                };
            }

            _logger.LogError("ZarinPal Logic Error. Errors: {Errors}", JsonSerializer.Serialize(responseContent?.Errors));
            return new PaymentRequestResultDto
            {
                IsSuccess = false,
                Message = responseContent?.Data?.Message ?? "خطای درگاه پرداخت"
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "ZarinPal Request Timeout for Order {OrderId}", initiationDto.OrderId);
            return new PaymentRequestResultDto { IsSuccess = false, Message = "زمان پاسخگویی درگاه به پایان رسید" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal Request Exception for Order {OrderId}", initiationDto.OrderId);
            return new PaymentRequestResultDto { IsSuccess = false, Message = "خطای سیستمی در درخواست پرداخت" };
        }
    }

    public async Task<GatewayVerificationResultDto> VerifyPaymentAsync(decimal amount, string authority)
    {
        var verifyUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment/verify.json"
            : "https://payment.zarinpal.com/pg/v4/payment/verify.json";

        var verifyPayload = new ZarinpalVerificationRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amount,
            Authority = authority
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(verifyUrl, verifyPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ZarinPal Verify Failed with Status {StatusCode}. Authority: {Authority}. Content: {Content}",
                    response.StatusCode, authority, errorContent);
                return new GatewayVerificationResultDto { IsVerified = false, Message = "خطا در ارتباط با درگاه جهت تایید" };
            }

            var responseContent = await response.Content.ReadFromJsonAsync<ZarinpalVerificationResponseDto>();

            if (responseContent?.Data != null && (responseContent.Data.Code == 100 || responseContent.Data.Code == 101))
            {
                return new GatewayVerificationResultDto
                {
                    IsVerified = true,
                    RefId = responseContent.Data.RefID,
                    CardPan = responseContent.Data.CardPan,
                    CardHash = responseContent.Data.CardHash,
                    Fee = responseContent.Data.Fee,
                    Message = responseContent.Data.Code == 101 ? "Verified (Already)" : "Verified"
                };
            }

            _logger.LogWarning("ZarinPal Verification Logic Fail. Authority: {Authority}, Code: {Code}", authority, responseContent?.Data?.Code);

            return new GatewayVerificationResultDto
            {
                IsVerified = false,
                Message = GetZarinPalErrorMessage(responseContent?.Data?.Code ?? -1)
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "ZarinPal Verify Timeout for Authority {Authority}", authority);
            return new GatewayVerificationResultDto { IsVerified = false, Message = "زمان پاسخگویی درگاه به پایان رسید" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZarinPal Verify Exception for Authority {Authority}", authority);
            return new GatewayVerificationResultDto { IsVerified = false, Message = "خطای سیستمی در تایید پرداخت" };
        }
    }

    private string GetZarinPalErrorMessage(int code)
    {
        return code switch
        {
            -50 => "مبلغ پرداخت شده با مقدار مبلغ در درگاه متفاوت است.",
            -51 => "پرداخت ناموفق.",
            -52 => "خطای غیر منتظره با پشتیبانی تماس بگیرید.",
            -53 => "اتوریتی برای این مرچنت کد نیست.",
            -54 => "اتوریتی نامعتبر است.",
            101 => "تراکنش قبلا تایید شده است.",
            _ => "خطای ناشناخته در پرداخت."
        };
    }
}