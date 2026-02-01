namespace Domain.Services;

public class PriceCalculatorService
{
    public (decimal FinalPrice, decimal DiscountAmount, bool HasDiscount) CalculateFinalPrice(ProductVariant variant, DiscountCode? discountCode = null)
    {
        decimal basePrice = variant.SellingPrice; decimal finalPrice = basePrice; decimal discountAmount = 0;

        // 1. Calculate Product Level Discount (if OriginalPrice > SellingPrice)
        if (variant.OriginalPrice > variant.SellingPrice)
        {
            // Logic handled by data structure, but can be enforced here
        }

        // 2. Apply Coupon Discount
        if (discountCode != null && discountCode.IsActive)
        {
            if (discountCode.Percentage > 0)
            {
                var calcDiscount = (basePrice * discountCode.Percentage) / 100;
                if (discountCode.MaxDiscountAmount.HasValue && calcDiscount > discountCode.MaxDiscountAmount.Value)
                {
                    calcDiscount = discountCode.MaxDiscountAmount.Value;
                }
                discountAmount += calcDiscount;
            }
        }

        finalPrice -= discountAmount;
        if (finalPrice < 0) finalPrice = 0;

        return (finalPrice, discountAmount, discountAmount > 0 || variant.OriginalPrice > variant.SellingPrice);
    }
}