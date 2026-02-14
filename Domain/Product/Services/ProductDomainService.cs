namespace Domain.Product.Services;

/// <summary>
/// Domain Service برای عملیات‌هایی که بین چند Aggregate هستند
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class ProductDomainService
{
    /// <summary>
    /// اعتبارسنجی امکان خرید محصول
    /// </summary>
    public ProductPurchaseValidation ValidateForPurchase(
        Product product,
        int variantId,
        int quantity)
    {
        Guard.Against.Null(product, nameof(product));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (product.IsDeleted)
            return ProductPurchaseValidation.Failed("محصول حذف شده است.");

        if (!product.IsActive)
            return ProductPurchaseValidation.Failed("محصول غیرفعال است.");

        var variant = product.FindVariant(variantId);
        if (variant == null)
            return ProductPurchaseValidation.Failed("واریانت یافت نشد.");

        if (!variant.IsActive)
            return ProductPurchaseValidation.Failed("واریانت غیرفعال است.");

        if (!variant.CanFulfill(quantity))
        {
            return ProductPurchaseValidation.InsufficientStock(
                variant.AvailableStock,
                quantity);
        }

        return ProductPurchaseValidation.Success(
            Money.FromDecimal(variant.SellingPrice),
            variant.AvailableStock);
    }

    /// <summary>
    /// محاسبه قیمت نهایی با اعمال تخفیف
    /// </summary>
    public Money CalculateFinalPrice(
        Product product,
        int variantId,
        int quantity,
        DiscountCode? discountCode = null)
    {
        Guard.Against.Null(product, nameof(product));

        var variant = product.FindVariant(variantId);
        if (variant == null)
            throw new DomainException("واریانت یافت نشد.");

        var unitPrice = product.CalculateSellingPrice(variantId, discountCode);
        return unitPrice.Multiply(quantity);
    }

    /// <summary>
    /// بررسی موجودی چند واریانت به صورت همزمان
    /// </summary>
    public BatchStockValidation ValidateBatchStock(
        IEnumerable<(Product Product, int VariantId, int Quantity)> items)
    {
        Guard.Against.Null(items, nameof(items));

        var results = new List<VariantStockValidationResult>();
        var allAvailable = true;

        foreach (var (product, variantId, quantity) in items)
        {
            var validation = ValidateForPurchase(product, variantId, quantity);

            var result = new VariantStockValidationResult(
                product.Id,
                variantId,
                quantity,
                validation.IsValid,
                validation.AvailableStock,
                validation.Error);

            results.Add(result);

            if (!validation.IsValid)
                allAvailable = false;
        }

        return new BatchStockValidation(results, allAvailable);
    }

    /// <summary>
    /// محاسبه آمار محصول
    /// </summary>
    public ProductStatistics CalculateStatistics(Product product)
    {
        Guard.Against.Null(product, nameof(product));

        var activeVariants = product.Variants.Where(v => !v.IsDeleted && v.IsActive).ToList();
        var allVariants = product.Variants.Where(v => !v.IsDeleted).ToList();

        var totalStock = allVariants.Where(v => !v.IsUnlimited).Sum(v => v.AvailableStock);
        var lowStockCount = allVariants.Count(v => v.IsLowStock);
        var outOfStockCount = allVariants.Count(v => v.IsOutOfStock);
        var unlimitedCount = allVariants.Count(v => v.IsUnlimited);

        var avgPrice = activeVariants.Any()
            ? activeVariants.Average(v => v.SellingPrice)
            : 0;

        var maxDiscount = activeVariants.Any(v => v.HasDiscount)
            ? activeVariants.Where(v => v.HasDiscount).Max(v => v.DiscountPercentage)
            : 0;

        return new ProductStatistics(
            TotalVariants: allVariants.Count,
            ActiveVariants: activeVariants.Count,
            TotalStock: totalStock,
            LowStockVariants: lowStockCount,
            OutOfStockVariants: outOfStockCount,
            UnlimitedVariants: unlimitedCount,
            AveragePrice: Math.Round(avgPrice, 0),
            MaxDiscountPercentage: maxDiscount,
            AverageRating: product.AverageRating,
            ReviewCount: product.ReviewCount,
            SalesCount: product.SalesCount);
    }

    /// <summary>
    /// پیشنهاد قیمت بر اساس قواعد بیزینس
    /// </summary>
    public PricingSuggestion SuggestPricing(
        decimal purchasePrice,
        decimal desiredProfitMargin = 30)
    {
        if (purchasePrice <= 0)
            throw new DomainException("قیمت خرید باید بزرگتر از صفر باشد.");

        if (desiredProfitMargin < 0 || desiredProfitMargin > 200)
            throw new DomainException("درصد سود باید بین ۰ تا ۲۰۰ باشد.");

        var suggestedSellingPrice = purchasePrice * (1 + desiredProfitMargin / 100);
        suggestedSellingPrice = Math.Ceiling(suggestedSellingPrice / 1000) * 1000; // Round to nearest 1000

        var actualProfitMargin = ((suggestedSellingPrice - purchasePrice) / purchasePrice) * 100;

        return new PricingSuggestion(
            PurchasePrice: purchasePrice,
            SuggestedSellingPrice: suggestedSellingPrice,
            SuggestedOriginalPrice: suggestedSellingPrice,
            ActualProfitMargin: Math.Round(actualProfitMargin, 2),
            ProfitAmount: suggestedSellingPrice - purchasePrice);
    }
}

#region Result Types

public sealed class ProductPurchaseValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public Money? UnitPrice { get; private set; }
    public int? AvailableStock { get; private set; }
    public int? RequestedQuantity { get; private set; }

    private ProductPurchaseValidation() { }

    public static ProductPurchaseValidation Success(Money unitPrice, int availableStock)
    {
        return new ProductPurchaseValidation
        {
            IsValid = true,
            UnitPrice = unitPrice,
            AvailableStock = availableStock
        };
    }

    public static ProductPurchaseValidation Failed(string error)
    {
        return new ProductPurchaseValidation
        {
            IsValid = false,
            Error = error
        };
    }

    public static ProductPurchaseValidation InsufficientStock(int available, int requested)
    {
        return new ProductPurchaseValidation
        {
            IsValid = false,
            Error = $"موجودی کافی نیست. موجودی: {available}، درخواستی: {requested}",
            AvailableStock = available,
            RequestedQuantity = requested
        };
    }

    public int GetShortage() =>
        RequestedQuantity.HasValue && AvailableStock.HasValue
            ? Math.Max(0, RequestedQuantity.Value - AvailableStock.Value)
            : 0;
}

public sealed record VariantStockValidationResult(
    int ProductId,
    int VariantId,
    int RequestedQuantity,
    bool IsValid,
    int? AvailableStock,
    string? Error)
{
    public int Shortage => IsValid ? 0 : Math.Max(0, RequestedQuantity - (AvailableStock ?? 0));
}

public sealed class BatchStockValidation
{
    public IReadOnlyList<VariantStockValidationResult> Results { get; }
    public bool AllAvailable { get; }
    public int TotalShortage { get; }

    public BatchStockValidation(IReadOnlyList<VariantStockValidationResult> results, bool allAvailable)
    {
        Results = results;
        AllAvailable = allAvailable;
        TotalShortage = results.Sum(r => r.Shortage);
    }

    public IEnumerable<VariantStockValidationResult> GetUnavailableItems()
    {
        return Results.Where(r => !r.IsValid);
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
        TotalVariants > 0 ? Math.Round((decimal)ActiveVariants / TotalVariants * 100, 2) : 0;

    public bool HasStockIssues => LowStockVariants > 0 || OutOfStockVariants > 0;

    public bool IsHealthy => OutOfStockVariants == 0 && LowStockVariants < TotalVariants * 0.2m;
}

public sealed record PricingSuggestion(
    decimal PurchasePrice,
    decimal SuggestedSellingPrice,
    decimal SuggestedOriginalPrice,
    decimal ActualProfitMargin,
    decimal ProfitAmount);

#endregion