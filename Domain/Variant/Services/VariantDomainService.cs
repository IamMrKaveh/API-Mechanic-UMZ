using Domain.Common.Shared.ValueObjects;

namespace Domain.Variant.Services;

public sealed class VariantDomainService
{
    public ProductPurchaseValidation ValidateForPurchase(
        Domain.Product.Product product,
        ProductVariant variant,
        int quantity)
    {
        Domain.Common.Gaurd.Guard.Against.Null(product, nameof(product));
        Domain.Common.Gaurd.Guard.Against.Null(variant, nameof(variant));
        Domain.Common.Gaurd.Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (product.IsDeleted)
            return ProductPurchaseValidation.Failed("محصول حذف شده است.");

        if (!product.IsActive)
            return ProductPurchaseValidation.Failed("محصول غیرفعال است.");

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

    public Money CalculateFinalPrice(
        ProductVariant variant,
        int quantity,
        Domain.Discount.DiscountCode? discountCode = null)
    {
        Domain.Common.Gaurd.Guard.Against.Null(variant, nameof(variant));

        var price = Money.FromDecimal(variant.SellingPrice);

        if (discountCode != null && discountCode.IsCurrentlyValid())
        {
            price = discountCode.CalculateDiscountMoney(price);
        }

        return price.Multiply(quantity);
    }

    public BatchStockValidation ValidateBatchStock(
        System.Collections.Generic.IEnumerable<(Domain.Product.Product Product, ProductVariant Variant, int Quantity)> items)
    {
        Domain.Common.Gaurd.Guard.Against.Null(items, nameof(items));

        var results = new System.Collections.Generic.List<VariantStockValidationResult>();
        var allAvailable = true;

        foreach (var (product, variant, quantity) in items)
        {
            var validation = ValidateForPurchase(product, variant, quantity);

            var result = new VariantStockValidationResult(
                product.Id,
                variant.Id,
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
}

public sealed class ProductPurchaseValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public Money? UnitPrice { get; private set; }
    public int? AvailableStock { get; private set; }
    public int? RequestedQuantity { get; private set; }

    private ProductPurchaseValidation()
    { }

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
}

public sealed record VariantStockValidationResult(
    int ProductId,
    int VariantId,
    int RequestedQuantity,
    bool IsValid,
    int? AvailableStock,
    string? Error);

public sealed class BatchStockValidation
{
    public System.Collections.Generic.IReadOnlyList<VariantStockValidationResult> Results { get; }
    public bool AllAvailable { get; }
    public int TotalShortage { get; }

    public BatchStockValidation(System.Collections.Generic.IReadOnlyList<VariantStockValidationResult> results, bool allAvailable)
    {
        Results = results;
        AllAvailable = allAvailable;
        TotalShortage = System.Linq.Enumerable.Sum(results, r => r.IsValid ? 0 : System.Math.Max(0, r.RequestedQuantity - (r.AvailableStock ?? 0)));
    }
}