namespace Application.Payment.Contracts;

/// <summary>
/// اینترفیس اختیاری برای درگاه‌هایی که Refund API دارند.
/// </summary>
public interface IRefundableGateway
{
    Task<GatewayRefundResultDto> RefundAsync(
        string originalRefId,
        int amount,
        string reason,
        CancellationToken ct = default);
}