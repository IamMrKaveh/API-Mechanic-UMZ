namespace Application.Services;

public class DiscountService : IDiscountService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly LedkaContext _context;
    private readonly ILogger<DiscountService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public DiscountService(
        IDiscountRepository discountRepository,
        LedkaContext context,
        ILogger<DiscountService> logger,
        IUnitOfWork unitOfWork)
    {
        _discountRepository = discountRepository;
        _context = context;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<(Domain.Discount.DiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal)
    {
        var discount = await _discountRepository.GetDiscountByCodeForUpdateAsync(code);

        if (discount == null)
            return (null, "کد تخفیف نامعتبر است.");

        if (!discount.IsActive)
            return (null, "کد تخفیف غیرفعال است.");

        if (discount.ExpiresAt.HasValue && discount.ExpiresAt.Value < DateTime.UtcNow)
            return (null, "کد تخفیف منقضی شده است.");

        if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit.Value)
            return (null, "ظرفیت استفاده از کد تخفیف تمام شده است.");

        if (discount.MinOrderAmount.HasValue && orderTotal < discount.MinOrderAmount.Value)
            return (null, $"حداقل مبلغ سفارش برای استفاده از این کد تخفیف {discount.MinOrderAmount.Value:N0} تومان است.");

        var userRestriction = discount.Restrictions.FirstOrDefault(r => r.RestrictionType == "User");
        if (userRestriction != null && userRestriction.EntityId != userId)
            return (null, "این کد تخفیف برای شما قابل استفاده نیست.");

        var categoryRestriction = discount.Restrictions.FirstOrDefault(r => r.RestrictionType == "Category");
        if (categoryRestriction != null)
        {
            return (null, "این کد تخفیف فقط برای دسته‌بندی خاصی قابل استفاده است.");
        }

        var userUsageCount = await _context.DiscountUsages
            .CountAsync(du => du.DiscountCodeId == discount.Id && du.UserId == userId && du.IsConfirmed);

        var maxUserUsage = discount.Restrictions
            .FirstOrDefault(r => r.RestrictionType == "MaxUserUsage")?.EntityId ?? 1;

        if (userUsageCount >= maxUserUsage)
            return (null, "شما قبلاً از این کد تخفیف استفاده کرده‌اید.");

        return (discount, null);
    }

    public async Task<ServiceResult<DiscountApplyResultDto>> ValidateAndApplyDiscountAsync(string code, decimal orderTotal, int userId)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var (discount, error) = await ValidateAndGetDiscountAsync(code, userId, orderTotal);

                if (error != null || discount == null)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<DiscountApplyResultDto>.Fail(error ?? "خطا در اعتبارسنجی کد تخفیف");
                }

                var discountAmount = (orderTotal * discount.Percentage) / 100;

                if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
                    discountAmount = discount.MaxDiscountAmount.Value;

                discount.UsedCount++;

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Discount code {Code} applied for user {UserId}. Amount: {DiscountAmount}",
                    code, userId, discountAmount);

                return ServiceResult<DiscountApplyResultDto>.Ok(new DiscountApplyResultDto
                {
                    DiscountAmount = discountAmount,
                    DiscountCodeId = discount.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error applying discount code {Code} for user {UserId}", code, userId);
                return ServiceResult<DiscountApplyResultDto>.Fail("خطای سیستمی در اعمال کد تخفیف");
            }
        });
    }

    public async Task RollbackDiscountUsageAsync(int discountCodeId)
    {
        try
        {
            var discount = await _context.DiscountCodes.FindAsync(discountCodeId);

            if (discount != null && discount.UsedCount > 0)
            {
                discount.UsedCount--;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Discount usage rolled back for code ID: {DiscountCodeId}", discountCodeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rolling back discount usage for code ID: {DiscountCodeId}", discountCodeId);
        }
    }

    public async Task ConfirmDiscountUsageAsync(int orderId)
    {
        try
        {
            var discountUsage = await _context.DiscountUsages
                .FirstOrDefaultAsync(du => du.OrderId == orderId && !du.IsConfirmed);

            if (discountUsage != null)
            {
                discountUsage.IsConfirmed = true;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Discount usage confirmed for order ID: {OrderId}", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming discount usage for order ID: {OrderId}", orderId);
        }
    }

    public async Task CancelDiscountUsageAsync(int orderId)
    {
        try
        {
            var discountUsage = await _context.DiscountUsages
                .Include(du => du.DiscountCode)
                .FirstOrDefaultAsync(du => du.OrderId == orderId);

            if (discountUsage != null)
            {
                if (discountUsage.DiscountCode != null && discountUsage.DiscountCode.UsedCount > 0)
                {
                    discountUsage.DiscountCode.UsedCount--;
                }

                _context.DiscountUsages.Remove(discountUsage);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Discount usage cancelled for order ID: {OrderId}", orderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling discount usage for order ID: {OrderId}", orderId);
        }
    }
}