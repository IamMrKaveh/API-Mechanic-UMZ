namespace Domain.Services;

public class PricingService : IPricingService
{
    public decimal CalculateFinalPrice(decimal originalPrice, decimal? discountPercentage, decimal? fixedDiscountAmount)
    {
        var price = originalPrice;

        if (discountPercentage.HasValue && discountPercentage.Value > 0)
        {
            price -= price * (discountPercentage.Value / 100);
        }

        if (fixedDiscountAmount.HasValue && fixedDiscountAmount.Value > 0)
        {
            price -= fixedDiscountAmount.Value;
        }

        return price < 0 ? 0 : price;
    }

    public bool ValidatePriceConsistency(decimal originalPrice, decimal sellingPrice)
    {
        if (sellingPrice < 0 || originalPrice < 0) return false;
        if (sellingPrice > originalPrice) return false;
        return true;
    }
}