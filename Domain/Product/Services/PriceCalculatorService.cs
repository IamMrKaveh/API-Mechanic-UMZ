namespace Domain.Product.Services;

public class PriceCalculatorService
{
    public void ValidatePriceConsistency(decimal purchasePrice, decimal sellingPrice, decimal? originalPrice)
    {
        if (sellingPrice < purchasePrice)
        {
            throw new DomainException("Selling price cannot be less than purchase price (Loss violation).");
        }

        if (originalPrice.HasValue && sellingPrice > originalPrice.Value)
        {
            throw new DomainException("Selling price cannot be greater than original (pre-discount) price.");
        }
    }
}