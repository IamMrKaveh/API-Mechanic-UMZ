namespace Domain.Common.Services;

public interface IPricingService
{
    decimal CalculateFinalPrice(decimal basePrice, decimal? discountAmount, bool isPercentage);
}