using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Services.Results;

public sealed class LowStockCheckResult
{
    public bool IsLowStock { get; }
    public bool IsOutOfStock { get; }
    public bool IsNotApplicable { get; }
    public VariantId? VariantId { get; }
    public int? CurrentStock { get; }
    public int? Threshold { get; }

    private LowStockCheckResult(
        bool isLowStock = false,
        bool isOutOfStock = false,
        bool isNotApplicable = false,
        VariantId? variantId = null,
        int? currentStock = null,
        int? threshold = null)
    {
        IsLowStock = isLowStock;
        IsOutOfStock = isOutOfStock;
        IsNotApplicable = isNotApplicable;
        VariantId = variantId;
        CurrentStock = currentStock;
        Threshold = threshold;
    }

    public static LowStockCheckResult NotApplicable() => new(isNotApplicable: true);

    public static LowStockCheckResult Healthy(int currentStock) => new(currentStock: currentStock);

    public static LowStockCheckResult OutOfStock(VariantId variantId, int stock)
        => new(isOutOfStock: true, variantId: variantId, currentStock: stock);

    public static LowStockCheckResult LowStock(VariantId variantId, int stock, int threshold)
        => new(isLowStock: true, variantId: variantId, currentStock: stock, threshold: threshold);
}