using Domain.Discount.ValueObjects;

namespace Domain.Common.Services;

public class PricingService : IPricingService
{
    public Money CalculateFinalPrice(Money basePrice, DiscountValue? discount)
    {
        if (discount is null)
            return basePrice;

        return discount.Apply(basePrice);
    }
}