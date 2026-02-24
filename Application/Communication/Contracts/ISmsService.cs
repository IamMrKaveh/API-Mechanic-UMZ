namespace Application.Communication.Contracts;

/// <summary>
/// سرویس ارسال پیامک
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// ارسال پیامک ساده
    /// </summary>
    Task<SmsResult> SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken ct = default
        );

    Task SendOrderConfirmationSmsAsync(
        string phoneNumber,
        string orderNumber,
        decimal totalAmount,
        CancellationToken ct = default
        );

    Task SendPaymentSuccessSmsAsync(
        string phoneNumber,
        string orderNumber,
        string refId,
        CancellationToken ct = default
        );
}

public class SmsResult
{
    public bool IsSuccess { get; private set; }
    public bool IsFailed { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? MessageId { get; private set; }

    private SmsResult()
    { }

    public static SmsResult Success(
        string? messageId = null
        )
        => new()
        {
            IsSuccess = true,
            IsFailed = false,
            MessageId = messageId
        };

    public static SmsResult Failed(
        string errorMessage
        )
        => new()
        {
            IsFailed = true,
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
}