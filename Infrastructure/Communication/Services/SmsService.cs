namespace Infrastructure.Communication.Services;

public class SmsService : ISmsService
{
    private readonly ILogger<SmsService> _logger;
    private readonly IConfiguration _configuration;

    public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<SmsResult> SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        try
        {
            var apiKey = _configuration["Kavenegar:ApiKey"] ?? "6C43574D53556774665763527167557A75376D39687A7935666A78353777783238704A302F7053303367383D";

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Kavenegar ApiKey is not configured.");
                return SmsResult.Failed("تنظیمات سرویس پیامک ناقص است.");
            }

            var isVerifyTemplate = message.All(char.IsDigit) && message.Length >= 4 && message.Length <= 6;
            var url = isVerifyTemplate
                ? $"https://api.kavenegar.com/v1/{apiKey}/verify/lookup.json"
                : $"https://api.kavenegar.com/v1/{apiKey}/sms/send.json";

            using var http = new HttpClient();

            var data = new Dictionary<string, string>();
            if (isVerifyTemplate)
            {
                data.Add("receptor", phoneNumber);
                data.Add("token", message);
                data.Add("template", "verify");
            }
            else
            {
                data.Add("receptor", phoneNumber);
                data.Add("message", message);
                data.Add("sender", _configuration["Kavenegar:SenderNumber"] ?? "10008663");
            }

            var response = await http.PostAsync(url, new FormUrlEncodedContent(data), ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent to {PhoneNumber} via Kavenegar.", MaskPhoneNumber(phoneNumber));
                return SmsResult.Success(Guid.NewGuid().ToString());
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Kavenegar failed. Status: {Status}, Content: {Content}", response.StatusCode, errorContent);
                return SmsResult.Failed($"خطا در ارسال پیامک: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return SmsResult.Failed($"خطا در ارسال پیامک: {ex.Message}");
        }
    }

    public async Task SendOrderConfirmationSmsAsync(
        string phoneNumber,
        string orderNumber,
        decimal totalAmount,
        CancellationToken ct = default)
    {
        var message = $"سفارش شماره {orderNumber} به مبلغ {totalAmount:N0} تومان با موفقیت ثبت شد. - فروشگاه لدکا";
        await SendSmsAsync(phoneNumber, message, ct);
    }

    public async Task SendPaymentSuccessSmsAsync(
        string phoneNumber,
        string orderNumber,
        string refId,
        CancellationToken ct = default)
    {
        var message = $"پرداخت سفارش {orderNumber} موفق بود. کد پیگیری: {refId} - فروشگاه لدکا";
        await SendSmsAsync(phoneNumber, message, ct);
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 7)
            return "***";

        return phoneNumber[..4] + "***" + phoneNumber[^3..];
    }
}