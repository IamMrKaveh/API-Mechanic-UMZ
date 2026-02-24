namespace Domain.Product.ValueObjects;

public sealed class ProductStats : ValueObject
{
    public Money MinPrice { get; private set; }
    public Money MaxPrice { get; private set; }
    public int TotalStock { get; private set; }
    public decimal AverageRating { get; private set; }
    public int ReviewCount { get; private set; }
    public int SalesCount { get; private set; }

    private ProductStats()
    {
        MinPrice = Money.Zero();
        MaxPrice = Money.Zero();
    }

    public static ProductStats CreateEmpty()
    {
        return new ProductStats
        {
            MinPrice = Money.Zero(),
            MaxPrice = Money.Zero(),
            TotalStock = 0,
            AverageRating = 0,
            ReviewCount = 0,
            SalesCount = 0
        };
    }

    internal ProductStats Recalculate(IEnumerable<ProductVariant> activeVariants)
    {
        var variants = activeVariants.ToList();
        if (!variants.Any())
        {
            return CreateEmpty();
        }

        return new ProductStats
        {
            MinPrice = Money.FromDecimal(variants.Min(v => v.SellingPrice.Amount)),
            MaxPrice = Money.FromDecimal(variants.Max(v => v.SellingPrice.Amount)),
            TotalStock = variants.Where(v => !v.IsUnlimited).Sum(v => v.StockQuantity),
            AverageRating = AverageRating,
            ReviewCount = ReviewCount,
            SalesCount = SalesCount
        };
    }

    public ProductStats UpdateReviews(int count, decimal average)
    {
        return new ProductStats
        {
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            TotalStock = TotalStock,
            ReviewCount = count,
            AverageRating = average,
            SalesCount = SalesCount
        };
    }

    internal ProductStats IncrementSales(int quantity)
    {
        return new ProductStats
        {
            MinPrice = MinPrice,
            MaxPrice = MaxPrice,
            TotalStock = TotalStock,
            AverageRating = AverageRating,
            ReviewCount = ReviewCount,
            SalesCount = SalesCount + quantity
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinPrice;
        yield return MaxPrice;
        yield return TotalStock;
        yield return AverageRating;
        yield return ReviewCount;
        yield return SalesCount;
    }
}