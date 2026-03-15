using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Services.Results;

public sealed class AdjustmentResult
{
    public bool IsSuccess { get; }
    public ProductVariantId VariantId { get; }
    public int NewStock { get; }
    public string? Error { get; }
    public string? Message { get; }

    private AdjustmentResult(bool isSuccess, ProductVariantId variantId, int newStock = 0, string? error = null, string? message = null)
    {
        IsSuccess = isSuccess;
        VariantId = variantId;
        NewStock = newStock;
        Error = error;
        Message = message;
    }

    public static AdjustmentResult Success(ProductVariantId variantId, int newStock)
        => new(true, variantId, newStock);

    public static AdjustmentResult Failed(ProductVariantId variantId, string error)
        => new(false, variantId, error: error);

    public static AdjustmentResult NotApplicable(ProductVariantId variantId, string message)
        => new(true, variantId, message: message);
}