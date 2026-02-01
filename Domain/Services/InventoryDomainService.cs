namespace Domain.Services;

public class InventoryDomainService
{
    public bool CanDeductStock(ProductVariant variant, int quantity) { if (variant.IsUnlimited) return true; return variant.StockQuantity >= quantity; }

    public void DeductStock(ProductVariant variant, int quantity)
    {
        if (variant.IsUnlimited) return;

        if (variant.StockQuantity < quantity)
        {
            throw new InvalidOperationException($"Insufficient stock for variant {variant.Id}. Available: {variant.StockQuantity}, Requested: {quantity}");
        }

        variant.StockQuantity -= quantity;
    }

    public void RestoreStock(ProductVariant variant, int quantity)
    {
        if (variant.IsUnlimited) return;
        variant.StockQuantity += quantity;
    }
}