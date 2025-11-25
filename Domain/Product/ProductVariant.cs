namespace Domain.Product;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string? Sku { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock
    {
        get
        {
            return InventoryTransactions?.Sum(t => t.QuantityChange) ?? 0;
        }
        private set { }
    }
    public bool IsUnlimited { get; set; }
    public new bool IsActive { get; set; } = true;
    public new byte[]? RowVersion { get; set; }
    public new bool IsDeleted { get; set; }
    public new DateTime? DeletedAt { get; set; }
    public new int? DeletedBy { get; set; }

    public string DisplayName => string.Join(" / ", VariantAttributes.Select(va => va.AttributeValue.Value));
    public bool IsInStock => IsUnlimited || Stock > 0;
    public bool HasDiscount => OriginalPrice > SellingPrice;
    public decimal DiscountPercentage => OriginalPrice > 0 ? (OriginalPrice - SellingPrice) / OriginalPrice * 100 : 0;

    public ICollection<ProductVariantAttribute> VariantAttributes { get; set; } = new List<ProductVariantAttribute>();
    public ICollection<Media.Media> Images { get; set; } = new List<Media.Media>();
    public ICollection<Inventory.InventoryTransaction> InventoryTransactions { get; set; } = new List<Inventory.InventoryTransaction>();
    public ICollection<Cart.CartItem> CartItems { get; set; } = new List<Cart.CartItem>();
}