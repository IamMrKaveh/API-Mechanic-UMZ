namespace Application.Discount.Contracts;

/// <summary>
/// سرویس تخفیف - هماهنگی بین Domain و Infrastructure
/// </summary>
public interface IDiscountService
{
    /// <summary>
    /// اعتبارسنجی و اعمال کد تخفیف
    /// </summary>
    Task<ServiceResult<DiscountApplyResultDto>> ValidateAndApplyDiscountAsync(
        string code,
        decimal orderTotal,
        int userId,
        CancellationToken ct = default
        );

    /// <summary>
    /// لغو استفاده تخفیف برای یک سفارش
    /// </summary>
    Task<ServiceResult> CancelDiscountUsageAsync(
        int orderId,
        CancellationToken ct = default
        );

    /// <summary>
    /// تأیید استفاده تخفیف پس از پرداخت موفق
    /// </summary>
    Task<ServiceResult> ConfirmDiscountUsageAsync(
        int orderId,
        CancellationToken ct = default
        );
}