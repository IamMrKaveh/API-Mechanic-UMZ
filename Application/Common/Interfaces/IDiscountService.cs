namespace Application.Common.Interfaces;

public interface IDiscountService
{
    Task<(DiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal);
    Task<ServiceResult<DiscountApplyResultDto>> ValidateAndApplyDiscountAsync(string code, decimal orderTotal, int userId);
}

public class DiscountApplyResultDto
{
    public decimal DiscountAmount { get; set; }
    public int DiscountCodeId { get; set; }
}