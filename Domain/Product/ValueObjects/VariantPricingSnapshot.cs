namespace Domain.Product.ValueObjects;

public sealed record VariantPricingSnapshot
{
    public decimal SellingPrice { get; }
    public int StockQuantity { get; }
    public bool IsUnlimited { get; }

    private VariantPricingSnapshot(decimal sellingPrice, int stockQuantity, bool isUnlimited)
    {
        SellingPrice = sellingPrice;
        StockQuantity = stockQuantity;
        IsUnlimited = isUnlimited;
    }

    public static VariantPricingSnapshot Create(decimal sellingPrice, int stockQuantity, bool isUnlimited)
    {
        if (sellingPrice < 0)
            throw new DomainException("Selling price cannot be negative.");

        if (!isUnlimited && stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        return new VariantPricingSnapshot(sellingPrice, stockQuantity, isUnlimited);
    }
}