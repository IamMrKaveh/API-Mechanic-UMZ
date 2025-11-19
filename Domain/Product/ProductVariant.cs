namespace Domain.Product;

public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string? Sku { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal OriginalPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public int Stock { get; private protected set; }

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

    public ICollection<ProductVariantAttribute> VariantAttributes { get; set; } = [];
    public ICollection<Media.Media> Images { get; set; } = [];
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];

    public InventoryTransaction? AdjustStock(int quantityChange, string transactionType, int? userId, string? notes, int? orderItemId = null)
    {
        if (IsUnlimited)
        {
            return null;
        }

        var stockBefore = Stock;
        var stockAfter = stockBefore + quantityChange;

        if (stockAfter < 0)
        {
            throw new InvalidOperationException($"Insufficient stock for variant {Id}. Current: {stockBefore}, Requested change: {quantityChange}");
        }

        Stock = stockAfter;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            VariantId = Id,
            TransactionType = transactionType,
            Quantity = quantityChange,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            OrderItemId = orderItemId,
            UserId = userId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        return transaction;
    }
}