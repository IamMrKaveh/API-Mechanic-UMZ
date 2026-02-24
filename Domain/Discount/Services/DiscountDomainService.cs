using Domain.Common.Shared.ValueObjects;

namespace Domain.Discount.Services;

/// <summary>
/// Domain Service برای عملیات‌های پیچیده بین چند Aggregate
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class DiscountDomainService
{
    /// <summary>
    /// اعتبارسنجی و اعمال تخفیف روی سفارش
    /// </summary>
    public DiscountApplicationSummary ApplyDiscountToOrder(
        DiscountCode discount,
        Money orderTotal,
        int userId,
        int userPreviousUsageCount)
    {
        Guard.Against.Null(discount, nameof(discount));
        Guard.Against.Null(orderTotal, nameof(orderTotal));

        var applicationResult = discount.TryApply(orderTotal.Amount, userId, userPreviousUsageCount);

        if (!applicationResult.IsSuccess)
        {
            return DiscountApplicationSummary.Failed(applicationResult.Error!);
        }

        var discountMoney = Money.FromDecimal(applicationResult.DiscountAmount);
        var effectivePercentage = discount.GetEffectivePercentage(orderTotal.Amount);

        return DiscountApplicationSummary.Success(
            discountMoney,
            effectivePercentage,
            discount.Percentage);
    }

    /// <summary>
    /// بررسی محدودیت‌های تخفیف برای یک سبد خرید
    /// </summary>
    public DiscountRestrictionCheck CheckRestrictions(
        DiscountCode discount,
        IEnumerable<int> productIds,
        IEnumerable<int> categoryIds)
    {
        Guard.Against.Null(discount, nameof(discount));

        if (!discount.HasAnyRestrictions())
        {
            return DiscountRestrictionCheck.NoRestrictions();
        }

        var restrictedProductIds = discount.GetRestrictedProductIds().ToList();
        var restrictedCategoryIds = discount.GetRestrictedCategoryIds().ToList();

        
        if (restrictedProductIds.Any())
        {
            var matchingProducts = productIds.Intersect(restrictedProductIds).ToList();
            if (!matchingProducts.Any())
            {
                return DiscountRestrictionCheck.Failed("این کد تخفیف برای محصولات انتخابی قابل استفاده نیست.");
            }
        }

        
        if (restrictedCategoryIds.Any())
        {
            var matchingCategories = categoryIds.Intersect(restrictedCategoryIds).ToList();
            if (!matchingCategories.Any())
            {
                return DiscountRestrictionCheck.Failed("این کد تخفیف برای دسته‌بندی‌های انتخابی قابل استفاده نیست.");
            }
        }

        return DiscountRestrictionCheck.Passed();
    }

    /// <summary>
    /// محاسبه بهترین تخفیف از بین چند تخفیف
    /// </summary>
    public DiscountCode? SelectBestDiscount(
        IEnumerable<DiscountCode> availableDiscounts,
        Money orderTotal,
        int userId,
        Func<int, int> getUserUsageCount)
    {
        Guard.Against.Null(availableDiscounts, nameof(availableDiscounts));
        Guard.Against.Null(orderTotal, nameof(orderTotal));

        DiscountCode? bestDiscount = null;
        decimal maxDiscountAmount = 0;

        foreach (var discount in availableDiscounts)
        {
            var usageCount = getUserUsageCount(discount.Id);
            var result = discount.TryApply(orderTotal.Amount, userId, usageCount);

            if (result.IsSuccess && result.DiscountAmount > maxDiscountAmount)
            {
                maxDiscountAmount = result.DiscountAmount;
                bestDiscount = discount;
            }
        }

        return bestDiscount;
    }

    /// <summary>
    /// محاسبه تخفیف قابل اعمال روی آیتم‌های خاص
    /// </summary>
    public Money CalculatePartialDiscount(
        DiscountCode discount,
        IEnumerable<(int ProductId, int CategoryId, Money Price)> items)
    {
        Guard.Against.Null(discount, nameof(discount));
        Guard.Against.Null(items, nameof(items));

        if (!discount.HasAnyRestrictions())
        {
            var total = items.Sum(i => i.Price.Amount);
            return Money.FromDecimal(discount.CalculateDiscountAmount(total));
        }

        var restrictedProductIds = discount.GetRestrictedProductIds().ToHashSet();
        var restrictedCategoryIds = discount.GetRestrictedCategoryIds().ToHashSet();

        decimal eligibleAmount = 0;

        foreach (var item in items)
        {
            var isEligible = false;

            if (restrictedProductIds.Any() && restrictedProductIds.Contains(item.ProductId))
                isEligible = true;

            if (restrictedCategoryIds.Any() && restrictedCategoryIds.Contains(item.CategoryId))
                isEligible = true;

            
            if (!restrictedProductIds.Any() && !restrictedCategoryIds.Any())
                isEligible = true;

            if (isEligible)
                eligibleAmount += item.Price.Amount;
        }

        return Money.FromDecimal(discount.CalculateDiscountAmount(eligibleAmount));
    }
}

#region Result Types

public sealed class DiscountApplicationSummary
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public Money DiscountAmount { get; private set; } = Money.Zero();
    public decimal EffectivePercentage { get; private set; }
    public decimal NominalPercentage { get; private set; }

    private DiscountApplicationSummary() { }

    public static DiscountApplicationSummary Success(
        Money discountAmount,
        decimal effectivePercentage,
        decimal nominalPercentage)
    {
        return new DiscountApplicationSummary
        {
            IsSuccess = true,
            DiscountAmount = discountAmount,
            EffectivePercentage = effectivePercentage,
            NominalPercentage = nominalPercentage
        };
    }

    public static DiscountApplicationSummary Failed(string error)
    {
        return new DiscountApplicationSummary
        {
            IsSuccess = false,
            Error = error
        };
    }

    public bool HasCap => EffectivePercentage < NominalPercentage;
}

public sealed class DiscountRestrictionCheck
{
    public bool IsPassed { get; private set; }
    public bool HasRestrictions { get; private set; }
    public string? Error { get; private set; }

    private DiscountRestrictionCheck() { }

    public static DiscountRestrictionCheck NoRestrictions()
    {
        return new DiscountRestrictionCheck
        {
            IsPassed = true,
            HasRestrictions = false
        };
    }

    public static DiscountRestrictionCheck Passed()
    {
        return new DiscountRestrictionCheck
        {
            IsPassed = true,
            HasRestrictions = true
        };
    }

    public static DiscountRestrictionCheck Failed(string error)
    {
        return new DiscountRestrictionCheck
        {
            IsPassed = false,
            HasRestrictions = true,
            Error = error
        };
    }
}

#endregion