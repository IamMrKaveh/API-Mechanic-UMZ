using System.Text;

namespace MainApi.Services.ZarinPal;

public class ZarinpalService : IZarinpalService
{
    private readonly HttpClient _httpClient;
    private readonly ZarinpalSettings _settings;
    private readonly ILogger<ZarinpalService> _logger;

    public ZarinpalService(HttpClient httpClient, IOptions<ZarinpalSettings> settings, ILogger<ZarinpalService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    private string GetApiUrl(string endpoint)
    {
        var baseUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment"
            : "https://api.zarinpal.com/pg/v4/payment";
        return $"{baseUrl}/{endpoint}.json";
    }

    public string GetPaymentGatewayUrl(string authority)
    {
        var baseUrl = _settings.IsSandbox
            ? "https://sandbox.zarinpal.com/pg/StartPay"
            : "https://www.zarinpal.com/pg/StartPay";
        return $"{baseUrl}/{authority}";
    }

    public async Task<ZarinpalRequestResponseDto?> CreatePaymentRequestAsync(decimal amount, string description, string callbackUrl, string? mobile = null, string? email = null)
    {
        var requestUrl = GetApiUrl("request");
        var requestDto = new ZarinpalRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amount,
            Description = description,
            CallbackURL = callbackUrl,
            Metadata = new ZarinpalMetadataDto
            {
                Mobile = mobile,
                Email = email
            }
        };

        var jsonContent = JsonSerializer.Serialize(requestDto);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(requestUrl, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal request failed with status {StatusCode}. Response: {Response}", response.StatusCode, responseContent);
                return null;
            }

            return JsonSerializer.Deserialize<ZarinpalRequestResponseDto>(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Zarinpal payment request.");
            return null;
        }
    }

    public async Task<ZarinpalVerificationResponseDataDto?> VerifyPaymentAsync(decimal amount, string authority)
    {
        var requestUrl = GetApiUrl("verify");
        var requestDto = new ZarinpalVerificationRequestDto
        {
            MerchantID = _settings.MerchantId,
            Amount = amount,
            Authority = authority
        };

        var jsonContent = JsonSerializer.Serialize(requestDto);
        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(requestUrl, httpContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal verification failed with status {StatusCode}. Response: {Response}", response.StatusCode, responseContent);
                return null;
            }

            var verificationResponse = JsonSerializer.Deserialize<ZarinpalVerificationResponseDto>(responseContent);
            return verificationResponse?.Data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Zarinpal payment verification.");
            return null;
        }
    }
}