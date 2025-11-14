namespace Domain.Product;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string? Sku { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public int Stock { get; set; }

    public bool IsUnlimited { get; set; }

    public bool IsActive { get; set; } = true;

    public int? MinOrderQuantity { get; set; }

    public int? MaxOrderQuantity { get; set; }

    public string DisplayName
    {
        get
        {
            var attributes = string.Join(" / ", VariantAttributes
                .OrderBy(a => a.AttributeValue.AttributeType.SortOrder)
                .Select(a => a.AttributeValue.DisplayValue));
            return string.IsNullOrWhiteSpace(attributes) ? Product.Name : $"{Product.Name} ({attributes})";
        }
    }

    public bool IsInStock => IsUnlimited || Stock > 0;

    public bool HasDiscount => OriginalPrice > SellingPrice;

    public double DiscountPercentage => OriginalPrice > 0 ? Math.Round((double)(1 - (SellingPrice / OriginalPrice)) * 100, 2) : 0;

    public ICollection<Attribute.ProductVariantAttribute> VariantAttributes { get; set; } = [];
    public ICollection<Media.Media> Images { get; set; } = [];
    public ICollection<Inventory.InventoryTransaction> InventoryTransactions { get; set; } = [];
    public ICollection<Cart.CartItem> CartItems { get; set; } = [];
}