namespace MainApi.Services.ZarinPal;
public class ZarinpalService : IZarinpalService
{
    private readonly HttpClient _httpClient;
    private readonly ZarinpalSettings _settings;
    private readonly ILogger<ZarinpalService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public ZarinpalService(HttpClient httpClient, IOptions<ZarinpalSettings> settings, ILogger<ZarinpalService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        var baseUrl = _settings.IsSandbox
            ?
"https://sandbox.zarinpal.com/pg/rest/WebGate/"
            : "https://api.zarinpal.com/pg/v4/payment/";
        _httpClient.BaseAddress = new Uri(baseUrl);
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };
    }

    public async Task<(string? paymentUrl, string? authority)> RequestPaymentAsync(decimal amount, string description, string callbackUrl, string? mobile, string? email)
    {
        try
        {
            var requestEndpoint = _settings.IsSandbox ?
"PaymentRequest.json" : "request.json";
            var requestDto = new ZarinpalRequestDto
            {
                MerchantID = _settings.MerchantId!,
                Amount = amount,
                Description = description,
                CallbackURL = callbackUrl,

                Mobile = mobile,
                Email = email
            };
            var jsonContent = JsonSerializer.Serialize(requestDto, _jsonSerializerOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal request failed. Status: {StatusCode}, Response: {ResponseBody}", response.StatusCode, responseBody);
                return (null, null);
            }

            var result = JsonSerializer.Deserialize<ZarinpalRequestResponseDto>(responseBody, _jsonSerializerOptions);
            if (result?.Status == 100 && !string.IsNullOrEmpty(result.Authority))
            {
                var paymentGatewayUrl = _settings.IsSandbox
                    ?
"https://sandbox.zarinpal.com/pg/StartPay/"
                    : "https://www.zarinpal.com/pg/StartPay/";

                var authorityHash = GenerateAuthorityHash(result.Authority, amount);
                var authorityToStore = $"{result.Authority}.{authorityHash}";
                _logger.LogInformation("Generated Hashed Authority for Order");

                return ($"{paymentGatewayUrl}{result.Authority}", authorityToStore);
            }

            var errorMessage = $"Zarinpal request returned status {result?.Status}.";
            if (result?.Errors != null && result.Errors.Any())
            {
                errorMessage += $" Errors: {string.Join(", ", result.Errors)}";
            }
            _logger.LogError(errorMessage);
            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while requesting payment from Zarinpal.");
            return (null, null);
        }
    }

    public async Task<(bool isSuccess, long? refId)> VerifyPaymentAsync(string authority, decimal amount)
    {
        try
        {
            var authorityParts = authority.Split('.');
            if (authorityParts.Length != 2 || !VerifyAuthority(authority, amount))
            {
                _logger.LogWarning("Invalid or tampered authority hash for amount {Amount}. Authority: {Authority}", amount, authority);
                return (false, null);
            }

            var originalAuthority = authorityParts[0];

            var verificationEndpoint = _settings.IsSandbox ?
"PaymentVerification.json" : "verification.json";
            var requestDto = new ZarinpalVerificationRequestDto
            {
                MerchantID = _settings.MerchantId!,
                Authority = originalAuthority,
                Amount = amount
            };
            var jsonContent = JsonSerializer.Serialize(requestDto, _jsonSerializerOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(verificationEndpoint, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Zarinpal verification failed. Status: {StatusCode}, Response: {ResponseBody}", response.StatusCode, responseBody);
                return (false, null);
            }

            var result = JsonSerializer.Deserialize<ZarinpalVerificationResponseDto>(responseBody, _jsonSerializerOptions);
            if (result != null && (result.Status == 100 || result.Status == 101))
            {
                _logger.LogInformation("Payment verification successful for Authority: {Authority}, RefID: {RefID}", authority, result.RefID);
                return (true, result.RefID);
            }

            var errorMessage = $"Zarinpal verification returned status {result?.Status} for Authority {authority}.";
            if (result?.Errors != null && result.Errors.Any())
            {
                errorMessage += $" Errors: {string.Join(", ", result.Errors)}";
            }
            _logger.LogWarning(errorMessage);
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during Zarinpal payment verification for Authority: {Authority}", authority);
            return (false, null);
        }
    }

    private string GenerateAuthorityHash(string authority, decimal amount)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.MerchantId!));
        var dataToHash = $"{authority}:{amount}";
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
        return Convert.ToBase64String(hashBytes);
    }

    private bool VerifyAuthority(string authority, decimal amount)
    {
        var parts = authority.Split('.');
        if (parts.Length != 2)
        {
            _logger.LogWarning("VerifyAuthority: Authority string '{Authority}' is not in the expected format 'auth.hash'", authority);
            return false;
        }

        var authBase = parts[0];
        var providedHash = parts[1];
        var expectedHash = GenerateAuthorityHash(authBase, amount);

        return providedHash == expectedHash;
    }
}