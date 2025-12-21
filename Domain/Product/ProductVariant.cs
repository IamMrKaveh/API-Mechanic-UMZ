namespace Domain.Product;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public int StockQuantity { get; set; }

    [NotMapped]
    public int Stock => StockQuantity;

    public bool IsUnlimited { get; set; }
    public new bool IsActive { get; set; } = true;
    public new byte[]? RowVersion { get; set; }
    public new bool IsDeleted { get; set; }
    public new DateTime? DeletedAt { get; set; }
    public new int? DeletedBy { get; set; }

    public string DisplayName => string.Join(" / ", VariantAttributes.Select(va => va.AttributeValue.Value));
    public bool IsInStock => IsUnlimited || StockQuantity > 0;
    public bool HasDiscount => OriginalPrice > SellingPrice;
    public decimal DiscountPercentage => OriginalPrice > 0 ? (OriginalPrice - SellingPrice) / OriginalPrice * 100 : 0;

    public ICollection<ProductVariantAttribute> VariantAttributes { get; set; } = new List<ProductVariantAttribute>();
    public ICollection<Media.Media> Images { get; set; } = new List<Media.Media>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<Inventory.InventoryTransaction>();
    public ICollection<CartItem> CartItems { get; set; } = new List<Cart.CartItem>();

    public void AdjustStock(int quantityChange)
    {
        if (!IsUnlimited)
        {
            var newStock = StockQuantity + quantityChange;
            if (newStock < 0)
            {
                throw new InvalidOperationException($"موجودی کافی نیست.  موجودی فعلی: {StockQuantity}");
            }
            StockQuantity = newStock;
        }
    }

    public int CalculateStockFromTransactions()
    {
        return InventoryTransactions.Sum(t => t.QuantityChange);
    }

    public bool ValidateStockConsistency()
    {
        var calculatedStock = CalculateStockFromTransactions();
        return StockQuantity == calculatedStock;
    }
}