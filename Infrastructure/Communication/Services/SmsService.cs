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
            // TODO: اتصال به سرویس پیامک واقعی (Kavenegar, Ghasedak, etc.)
            _logger.LogInformation(
                "SMS sent to {PhoneNumber}: {Message}",
                MaskPhoneNumber(phoneNumber),
                message);

            await Task.CompletedTask;

            return SmsResult.Success("mock-message-id");
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