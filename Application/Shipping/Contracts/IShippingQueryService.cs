using Application.Shipping.Features.Shared;

namespace Application.Shipping.Contracts;

/// <summary>
/// Read-side query service for shipping methods.
/// Returns DTOs directly — no domain entities.
/// </summary>
public interface IShippingQueryService
{
    /// <summary>
    /// دریافت روش‌های ارسال مجاز برای یک سبد خرید
    /// </summary>
    Task<IEnumerable<AvailableShippingMethodDto>> GetAvailableShippingMethodsForCartAsync(
        int userId,
        CancellationToken ct = default);

    /// <summary>
    /// محاسبه هزینه ارسال برای یک سبد خرید با روش ارسال مشخص
    /// </summary>
    Task<ShippingCostResultDto> CalculateShippingCostAsync(
        int userId,
        int shippingMethodId,
        CancellationToken ct = default);

    Task<IEnumerable<ShippingMethodDto>> GetActiveShippingMethodsAsync(CancellationToken ct = default);

    Task<ShippingMethodDto?> GetShippingMethodByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// دریافت روش‌های ارسال مجاز برای مجموعه‌ای از Variantها
    /// (بدون Cart)
    /// </summary>
    Task<IEnumerable<AvailableShippingMethodDto>> GetAvailableShippingMethodsForVariantsAsync(
        IReadOnlyCollection<int> variantIds,
        CancellationToken ct = default);
}