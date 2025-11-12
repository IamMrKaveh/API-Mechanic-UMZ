namespace MainApi.Services.Discount;

public class DiscountService : IDiscountService
{
    private readonly MechanicContext _context;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(MechanicContext context, ILogger<DiscountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(TDiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal)
    {
        var discount = await _context.TDiscountCode
            .Include(d => d.Restrictions)
            .FirstOrDefaultAsync(d => d.Code.ToLower() == code.ToLower() && d.IsActive);

        if (discount == null)
            return (null, "کد تخفیف نامعتبر است.");

        if (discount.ExpiresAt.HasValue && discount.ExpiresAt.Value < DateTime.UtcNow)
            return (null, "این کد تخفیف منقضی شده است.");

        if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit.Value)
            return (null, "ظرفیت استفاده از این کد تخفیف به پایان رسیده است.");

        if (discount.MinOrderAmount.HasValue && orderTotal < discount.MinOrderAmount.Value)
            return (null, $"حداقل مبلغ سفارش برای اعمال این کد تخفیف {discount.MinOrderAmount:N0} تومان است.");

        var userRestriction = discount.Restrictions.FirstOrDefault(r => r.RestrictionType == "User");
        if (userRestriction != null && userRestriction.EntityId != userId)
        {
            return (null, "این کد تخفیف برای شما تعریف نشده است.");
        }

        return (discount, null);
    }
}