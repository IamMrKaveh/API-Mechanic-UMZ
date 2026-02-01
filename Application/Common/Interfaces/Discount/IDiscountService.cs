using Application.DTOs.Discount;

namespace Application.Common.Interfaces.Discount;

public interface IDiscountService
{
    Task<(DiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal);
    Task<ServiceResult<DiscountApplyResultDto>> ValidateAndApplyDiscountAsync(string code, decimal orderTotal, int userId);
    Task RollbackDiscountUsageAsync(int discountCodeId);
    Task ConfirmDiscountUsageAsync(int orderId);
    Task CancelDiscountUsageAsync(int orderId);
}