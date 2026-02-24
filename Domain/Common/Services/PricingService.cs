namespace Domain.Common.Services;

public class PricingService : IPricingService
{
    public decimal CalculateFinalPrice(decimal basePrice, decimal? discountAmount, bool isPercentage)
    {
        if (!discountAmount.HasValue || discountAmount.Value == 0)
            return basePrice;

        if (isPercentage)
        {
            var discountValue = basePrice * (discountAmount.Value / 100);
            return basePrice - discountValue;
        }

        return basePrice - discountAmount.Value;
    }
}