namespace Domain.Product.Services;

public sealed class ProductDomainService
{
    public ProductStatistics CalculateStatistics(
        Product product,
        IEnumerable<ProductVariant> variants)
    {
        Guard.Against.Null(product, nameof(product));

        var variantList = Enumerable.ToList(variants);
        var activeVariants = Enumerable.ToList(Enumerable.Where(variantList, v => !v.IsDeleted && v.IsActive));
        var allVariants = Enumerable.ToList(Enumerable.Where(variantList, v => !v.IsDeleted));

        var totalStock = Enumerable.Sum(Enumerable.Where(allVariants, v => !v.IsUnlimited), v => v.AvailableStock);
        var lowStockCount = Enumerable.Count(allVariants, v => v.IsLowStock);
        var outOfStockCount = Enumerable.Count(allVariants, v => v.IsOutOfStock);
        var unlimitedCount = Enumerable.Count(allVariants, v => v.IsUnlimited);

        var avgPrice = Enumerable.Any(activeVariants)
            ? Enumerable.Average(activeVariants, v => v.SellingPrice)
            : 0;

        var maxDiscount = Enumerable.Any(activeVariants, v => v.HasDiscount)
            ? Enumerable.Max(Enumerable.Where(activeVariants, v => v.HasDiscount), v => v.DiscountPercentage)
            : 0;

        return new ProductStatistics(
            TotalVariants: allVariants.Count,
            ActiveVariants: activeVariants.Count,
            TotalStock: totalStock,
            LowStockVariants: lowStockCount,
            OutOfStockVariants: outOfStockCount,
            UnlimitedVariants: unlimitedCount,
            AveragePrice: System.Math.Round(avgPrice, 0),
            MaxDiscountPercentage: maxDiscount,
            AverageRating: product.Stats.AverageRating,
            ReviewCount: product.Stats.ReviewCount,
            SalesCount: product.Stats.SalesCount);
    }

    public PricingSuggestion SuggestPricing(
        decimal purchasePrice,
        decimal desiredProfitMargin = 30)
    {
        if (purchasePrice <= 0)
            throw new DomainException("قیمت خرید باید بزرگتر از صفر باشد.");

        if (desiredProfitMargin < 0 || desiredProfitMargin > 200)
            throw new DomainException("درصد سود باید بین ۰ تا ۲۰۰ باشد.");

        var suggestedSellingPrice = purchasePrice * (1 + desiredProfitMargin / 100);
        suggestedSellingPrice = System.Math.Ceiling(suggestedSellingPrice / 1000) * 1000;

        var actualProfitMargin = ((suggestedSellingPrice - purchasePrice) / purchasePrice) * 100;

        return new PricingSuggestion(
            PurchasePrice: purchasePrice,
            SuggestedSellingPrice: suggestedSellingPrice,
            SuggestedOriginalPrice: suggestedSellingPrice,
            ActualProfitMargin: System.Math.Round(actualProfitMargin, 2),
            ProfitAmount: suggestedSellingPrice - purchasePrice);
    }
}

public sealed record ProductStatistics(
    int TotalVariants,
    int ActiveVariants,
    int TotalStock,
    int LowStockVariants,
    int OutOfStockVariants,
    int UnlimitedVariants,
    decimal AveragePrice,
    decimal MaxDiscountPercentage,
    decimal AverageRating,
    int ReviewCount,
    int SalesCount)
{
    public decimal ActiveVariantsPercentage =>
        TotalVariants > 0 ? System.Math.Round((decimal)ActiveVariants / TotalVariants * 100, 2) : 0;

    public bool HasStockIssues => LowStockVariants > 0 || OutOfStockVariants > 0;

    public bool IsHealthy => OutOfStockVariants == 0 && LowStockVariants < TotalVariants * 0.2m;
}

public sealed record PricingSuggestion(
    decimal PurchasePrice,
    decimal SuggestedSellingPrice,
    decimal SuggestedOriginalPrice,
    decimal ActualProfitMargin,
    decimal ProfitAmount);