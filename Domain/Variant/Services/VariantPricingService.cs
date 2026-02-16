namespace Domain.Variant.Services;

public class VariantPricingService
{
    public void ValidatePricing(
        decimal purchasePrice,
        decimal sellingPrice,
        decimal originalPrice)
    {
        if (purchasePrice < 0)
            throw new DomainException("قیمت خرید نمی‌تواند منفی باشد.");

        if (sellingPrice < 0)
            throw new DomainException("قیمت فروش نمی‌تواند منفی باشد.");

        if (originalPrice < 0)
            throw new DomainException("قیمت اصلی نمی‌تواند منفی باشد.");

        if (sellingPrice < purchasePrice)
            throw new DomainException("قیمت فروش نمی‌تواند کمتر از قیمت خرید باشد.");

        if (sellingPrice > originalPrice && originalPrice > 0)
            throw new DomainException("قیمت فروش نمی‌تواند بیشتر از قیمت اصلی باشد.");
    }

    public decimal CalculateDiscountPercentage(decimal sellingPrice, decimal originalPrice)
    {
        if (originalPrice <= 0 || sellingPrice >= originalPrice)
            return 0;

        return Math.Round((1 - (sellingPrice / originalPrice)) * 100, 2);
    }

    public decimal CalculateProfitMargin(decimal sellingPrice, decimal purchasePrice)
    {
        if (purchasePrice <= 0)
            return 0;

        return Math.Round(((sellingPrice - purchasePrice) / purchasePrice) * 100, 2);
    }
}