namespace Application.Payment.Contracts;

/// <summary>
/// قرارداد برای درگاه‌های پرداخت - Adapter Pattern
/// هر درگاه (زرین‌پال، ملت، ...) این اینترفیس را پیاده‌سازی می‌کند
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// نام درگاه پرداخت
    /// </summary>
    string GatewayName { get; }

    /// <summary>
    /// ارسال درخواست پرداخت به درگاه و دریافت Authority و آدرس پرداخت
    /// </summary>
    Task<PaymentRequestResultDto> RequestPaymentAsync(
        decimal amount,
        string description,
        string callbackUrl,
        string? mobile = null,
        string? email = null);

    /// <summary>
    /// تأیید پرداخت پس از بازگشت کاربر از درگاه
    /// </summary>
    Task<GatewayVerificationResultDto> VerifyPaymentAsync(
        string authority,
        int amount);
}

/// <summary>
/// نتیجه درخواست پرداخت از درگاه
/// </summary>
public record PaymentRequestResultDto
{
    public bool IsSuccess { get; init; }
    public string? Authority { get; init; }
    public string? PaymentUrl { get; init; }
    public string? RedirectUrl { get; init; }
    public string? Message { get; init; }
    public string? RawResponse { get; init; }
}

/// <summary>
/// نتیجه تأیید پرداخت از درگاه
/// </summary>
public record GatewayVerificationResultDto
{
    public bool IsVerified { get; init; }
    public long? RefId { get; init; }
    public string? CardPan { get; init; }
    public string? CardHash { get; init; }
    public decimal Fee { get; init; }
    public string? Message { get; init; }
    public string? RawResponse { get; init; }
}