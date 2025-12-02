namespace Application.Services;

public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(IDiscountRepository discountRepository, ILogger<DiscountService> logger)
    {
        _discountRepository = discountRepository;
        _logger = logger;
    }

    public async Task<(Domain.Discount.DiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal)
    {
        var discount = await _discountRepository.GetDiscountByCodeForUpdateAsync(code); // Fix: Uses Update Lock

        if (discount == null) return (null, "کد تخفیف نامعتبر است.");
        if (discount.ExpiresAt.HasValue && discount.ExpiresAt.Value < DateTime.UtcNow) return (null, "منقضی شده.");
        if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit.Value) return (null, "تمام شده.");
        if (discount.MinOrderAmount.HasValue && orderTotal < discount.MinOrderAmount.Value) return (null, "مبلغ ناکافی.");

        var userRestriction = discount.Restrictions.FirstOrDefault(r => r.RestrictionType == "User");
        if (userRestriction != null && userRestriction.EntityId != userId) return (null, "نامعتبر برای کاربر.");

        return (discount, null);
    }

    public async Task<ServiceResult<DiscountApplyResultDto>> ValidateAndApplyDiscountAsync(string code, decimal orderTotal, int userId)
    {
        var (discount, error) = await ValidateAndGetDiscountAsync(code, userId, orderTotal);
        if (error != null || discount == null) return ServiceResult<DiscountApplyResultDto>.Fail(error ?? "Error");

        var discountAmount = (orderTotal * discount.Percentage) / 100;
        if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
            discountAmount = discount.MaxDiscountAmount.Value;

        return ServiceResult<DiscountApplyResultDto>.Ok(new DiscountApplyResultDto
        {
            DiscountAmount = discountAmount,
            DiscountCodeId = discount.Id
        });
    }

    public async Task RollbackDiscountUsageAsync(int discountCodeId)
    {
        // Implementation to decrement usage count safely
    }
}