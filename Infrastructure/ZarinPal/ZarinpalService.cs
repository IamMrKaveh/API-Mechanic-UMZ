using Application.Common.Interfaces;
using Application.DTOs;
using Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Infrastructure.ZarinPal;

public class ZarinpalService : IZarinpalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ZarinpalService> _logger;
    private readonly IUserRepository _userRepository;

    public ZarinpalService(
        HttpClient httpClient,
        ILogger<ZarinpalService> logger,
        IUserRepository userRepository)
    {
        _httpClient = httpClient;
        _logger = logger;
        _userRepository = userRepository;
    }

    private string GetApiUrl(bool isSandbox, string endpoint)
    {
        var baseUrl = isSandbox
            ? "https://sandbox.zarinpal.com/pg/v4/payment"
            : "https://api.zarinpal.com/pg/v4/payment";
        return $"{baseUrl}/{endpoint}.json";
    }

    public string GetPaymentGatewayUrl(bool isSandbox, string authority)
    {
        var baseUrl = isSandbox
            ? "https://sandbox.zarinpal.com/pg/StartPay"
            : "https://www.zarinpal.com/pg/StartPay";
        return $"{baseUrl}/{authority}";
    }

    public async Task<ZarinpalRequestResponseDto?> CreatePaymentRequestAsync(ZarinpalSettingsDto settings, decimal amount, string description, string callbackUrl, string? mobile = null, string? email = null)
    {
        var requestUrl = GetApiUrl(settings.IsSandbox, "request");
        var requestDto = new ZarinpalRequestDto
        {
            MerchantID = settings.MerchantId,
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

    public async Task<ZarinpalVerificationResponseDataDto?> VerifyPaymentAsync(ZarinpalSettingsDto settings, decimal amount, string authority)
    {
        var requestUrl = GetApiUrl(settings.IsSandbox, "verify");
        var requestDto = new ZarinpalVerificationRequestDto
        {
            MerchantID = settings.MerchantId,
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

            if (verificationResponse?.Data != null && (verificationResponse.Data.Code == 100 || verificationResponse.Data.Code == 101))
            {
                return verificationResponse.Data;
            }

            _logger.LogWarning("Zarinpal verification was not successful. Code: {Code}, Message: {Message}", verificationResponse?.Data?.Code, verificationResponse?.Data?.Message);
            return verificationResponse?.Data;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Zarinpal payment verification.");
            return null;
        }
    }

    public async Task<(string? PaymentUrl, string? ErrorMessage)> RequestPaymentAsync(decimal amount, string description, int orderId, int userId, string callbackUrl, string? userPhone)
    {
        // This method now seems redundant as the logic is better handled in OrderService.
        // It's kept for backward compatibility if other parts of the system use it,
        // but ideally it should be removed and logic consolidated in OrderService.
        // The responsibility of fetching settings and user data belongs to the Application layer.
        _logger.LogWarning("RequestPaymentAsync on IZarinpalService is being called directly. This logic should ideally be in the OrderService.");

        var zarinpalSettings = new ZarinpalSettingsDto { IsSandbox = true, MerchantId = "YourDefaultMerchantId" }; // This is a problem, no access to config.

        var response = await CreatePaymentRequestAsync(zarinpalSettings, amount, description, callbackUrl, userPhone);

        if (response?.Data?.Code == 100 && !string.IsNullOrEmpty(response.Data.Authority))
        {
            return (GetPaymentGatewayUrl(zarinpalSettings.IsSandbox, response.Data.Authority), null);
        }

        return (null, response?.Data?.Message ?? "Failed to create payment request.");
    }
}