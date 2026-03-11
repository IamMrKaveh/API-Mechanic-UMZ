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

    public static ProductStats Create(
        decimal minPrice,
        decimal maxPrice,
        int totalStock,
        decimal averageRating,
        int reviewCount,
        int salesCount)
    {
        if (minPrice < 0)
            throw new DomainException("Minimum price cannot be negative.");

        if (maxPrice < 0)
            throw new DomainException("Maximum price cannot be negative.");

        if (minPrice > maxPrice && maxPrice > 0)
            throw new DomainException("Minimum price cannot exceed maximum price.");

        if (totalStock < 0)
            throw new DomainException("Total stock cannot be negative.");

        if (averageRating < 0 || averageRating > 5)
            throw new DomainException("Average rating must be between 0 and 5.");

        if (reviewCount < 0)
            throw new DomainException("Review count cannot be negative.");

        if (salesCount < 0)
            throw new DomainException("Sales count cannot be negative.");

        return new ProductStats
        {
            MinPrice = Money.FromDecimal(minPrice),
            MaxPrice = Money.FromDecimal(maxPrice),
            TotalStock = totalStock,
            AverageRating = averageRating,
            ReviewCount = reviewCount,
            SalesCount = salesCount
        };
    }

    public ProductStats Recalculate(
        IEnumerable<VariantPricingSnapshot> activeVariantSnapshots)
    {
        var snapshots = activeVariantSnapshots.ToList();
        if (!snapshots.Any())
        {
            return CreateEmpty();
        }

        var minPrice = snapshots.Min(s => s.SellingPrice);
        var maxPrice = snapshots.Max(s => s.SellingPrice);
        var totalStock = snapshots
            .Where(s => !s.IsUnlimited)
            .Sum(s => s.StockQuantity);

        return new ProductStats
        {
            MinPrice = Money.FromDecimal(minPrice),
            MaxPrice = Money.FromDecimal(maxPrice),
            TotalStock = totalStock,
            AverageRating = AverageRating,
            ReviewCount = ReviewCount,
            SalesCount = SalesCount
        };
    }

    public ProductStats UpdateReviews(int count, decimal average)
    {
        if (count < 0)
            throw new DomainException("Review count cannot be negative.");

        if (average < 0 || average > 5)
            throw new DomainException("Average rating must be between 0 and 5.");

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

    public ProductStats IncrementSales(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Sales quantity must be greater than zero.");

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

    public bool HasStock => TotalStock > 0;

    public bool HasReviews => ReviewCount > 0;

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