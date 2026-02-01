namespace Domain.Services.Interfaces;

public interface IPricingService
{
    decimal CalculateFinalPrice(decimal originalPrice, decimal? discountPercentage, decimal? fixedDiscountAmount); 
    bool ValidatePriceConsistency(decimal originalPrice, decimal sellingPrice);
}