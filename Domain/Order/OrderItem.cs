namespace Domain.Order;

public class OrderItem : BaseEntity
{
    public int OrderId { get; private set; }
    public int VariantId { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public string? VariantSku { get; private set; }
    public string? VariantAttributes { get; private set; }
    public int Quantity { get; private set; }
    public Money PurchasePriceAtOrder { get; private set; } = null!;
    public Money SellingPriceAtOrder { get; private set; } = null!;
    public Money OriginalPriceAtOrder { get; private set; } = null!;
    public Money DiscountAtOrder { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;
    public Money Profit { get; private set; } = null!;

    // Navigation
    public Order? Order { get; private set; }

    public ProductVariant? Variant { get; private set; }

    public ICollection<InventoryTransaction> InventoryTransactions { get; private set; } = new List<InventoryTransaction>();

    private OrderItem()
    { }

    #region Factory Methods

    public static OrderItem CreateFromSnapshot(OrderItemSnapshot snapshot)
    {
        Guard.Against.Null(snapshot, nameof(snapshot));
        Guard.Against.NegativeOrZero(snapshot.Quantity, nameof(snapshot.Quantity));
        Guard.Against.Null(snapshot.PurchasePrice, nameof(snapshot.PurchasePrice));
        Guard.Against.Null(snapshot.SellingPrice, nameof(snapshot.SellingPrice));

        ValidatePricing(snapshot.PurchasePrice, snapshot.SellingPrice);

        var item = new OrderItem
        {
            VariantId = snapshot.VariantId,
            ProductId = snapshot.ProductId,
            ProductName = snapshot.ProductName,
            VariantSku = snapshot.VariantSku,
            VariantAttributes = snapshot.VariantAttributes,
            Quantity = snapshot.Quantity,
            PurchasePriceAtOrder = snapshot.PurchasePrice,
            SellingPriceAtOrder = snapshot.SellingPrice,
            OriginalPriceAtOrder = snapshot.OriginalPrice ?? snapshot.SellingPrice,
            DiscountAtOrder = CalculateDiscount(snapshot.OriginalPrice, snapshot.SellingPrice)
        };

        item.Recalculate();
        return item;
    }

    #endregion Factory Methods

    #region Query Methods

    public decimal GetUnitProfit() => SellingPriceAtOrder.Amount - PurchasePriceAtOrder.Amount;

    public decimal GetProfitMargin()
    {
        if (PurchasePriceAtOrder.Amount == 0) return 0;
        return Math.Round((GetUnitProfit() / PurchasePriceAtOrder.Amount) * 100, 2);
    }

    public decimal GetDiscountPercentage()
    {
        if (OriginalPriceAtOrder.Amount == 0) return 0;
        return Math.Round((DiscountAtOrder.Amount / OriginalPriceAtOrder.Amount) * 100, 2);
    }

    public bool HasDiscount() => DiscountAtOrder.Amount > 0;

    #endregion Query Methods

    #region Private Methods

    private void Recalculate()
    {
        Amount = SellingPriceAtOrder.Multiply(Quantity);
        Profit = Money.FromDecimal(GetUnitProfit()).Multiply(Quantity);
    }

    private static void ValidatePricing(Money purchasePrice, Money sellingPrice)
    {
        if (sellingPrice.Amount < purchasePrice.Amount)
        {
            throw new DomainException("قیمت فروش نمی‌تواند کمتر از قیمت خرید باشد.");
        }
    }

    private static Money CalculateDiscount(Money? originalPrice, Money sellingPrice)
    {
        if (originalPrice == null || originalPrice.Amount <= sellingPrice.Amount)
            return Money.Zero();

        return originalPrice.Subtract(sellingPrice);
    }

    #endregion Private Methods
}

/// <summary>
/// Price Snapshot برای ایجاد OrderItem
/// تضمین می‌کند که قیمت‌ها در لحظه ثبت سفارش ذخیره می‌شوند
/// </summary>
public sealed class OrderItemSnapshot
{
    public int VariantId { get; }
    public int ProductId { get; }
    public string ProductName { get; }
    public string? VariantSku { get; }
    public string? VariantAttributes { get; }
    public int Quantity { get; }
    public Money PurchasePrice { get; }
    public Money SellingPrice { get; }
    public Money? OriginalPrice { get; }

    private OrderItemSnapshot(
        int variantId,
        int productId,
        string productName,
        string? variantSku,
        string? variantAttributes,
        int quantity,
        Money purchasePrice,
        Money sellingPrice,
        Money? originalPrice)
    {
        VariantId = variantId;
        ProductId = productId;
        ProductName = productName;
        VariantSku = variantSku;
        VariantAttributes = variantAttributes;
        Quantity = quantity;
        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        OriginalPrice = originalPrice;
    }

    public static OrderItemSnapshot Create(
        int variantId,
        int productId,
        string productName,
        string? variantSku,
        string? variantAttributes,
        int quantity,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal? originalPrice = null)
    {
        Guard.Against.NegativeOrZero(variantId, nameof(variantId));
        Guard.Against.NegativeOrZero(productId, nameof(productId));
        Guard.Against.NullOrWhiteSpace(productName, nameof(productName));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        return new OrderItemSnapshot(
            variantId,
            productId,
            productName,
            variantSku,
            variantAttributes,
            quantity,
            Money.FromDecimal(purchasePrice),
            Money.FromDecimal(sellingPrice),
            originalPrice.HasValue ? Money.FromDecimal(originalPrice.Value) : null);
    }

    public static OrderItemSnapshot FromVariant(
        ProductVariant variant,
        int quantity)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        return new OrderItemSnapshot(
            variant.Id,
            variant.ProductId,
            variant.Product?.Name ?? "محصول",
            variant.Sku,
            variant.GetAttributesSummary(),
            quantity,
            Money.FromDecimal(variant.PurchasePrice),
            Money.FromDecimal(variant.SellingPrice),
            Money.FromDecimal(variant.OriginalPrice));
    }
}