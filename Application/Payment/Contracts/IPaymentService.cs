namespace Application.Payment.Contracts;

/// <summary>
/// سرویس پرداخت - هماهنگی بین Domain و Infrastructure
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// شروع فرآیند پرداخت
    /// </summary>
    Task<PaymentInitiationResultDto> InitiatePaymentAsync(
        PaymentInitiationDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// تأیید پرداخت از درگاه
    /// </summary>
    Task<ServiceResult<PaymentVerificationResultDto>> VerifyPaymentAsync(
        string authority,
        int amount,
        CancellationToken ct = default);
}

public class PaymentInitiationDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public Money Amount { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
}

public class PaymentInitiationResultDto
{
    public bool IsSuccess { get; set; }
    public string? Authority { get; set; }
    public string? PaymentUrl { get; set; }
    public string? Message { get; set; }
}

public class PaymentVerificationResultDto
{
    public bool IsVerified { get; set; }
    public long? RefId { get; set; }
    public string? CardPan { get; set; }
    public string? Message { get; set; }
}