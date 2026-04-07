using Domain.Variant.ValueObjects;
using SharedKernel.Results;

namespace Domain.Inventory.Services.Results;

public sealed class AdjustmentResult
{
    public bool IsSuccess { get; }
    public VariantId VariantId { get; }
    public int NewStock { get; }
    public string? Error { get; }
    public string? Message { get; }

    private AdjustmentResult(bool isSuccess, VariantId variantId, int newStock = 0, string? error = null, string? message = null)
    {
        IsSuccess = isSuccess;
        VariantId = variantId;
        NewStock = newStock;
        Error = error;
        Message = message;
    }

    public static AdjustmentResult Success(VariantId variantId, int newStock)
        => new(true, variantId, newStock);

    public static AdjustmentResult Failed(VariantId variantId, string error)
        => new(false, variantId, error: error);

    public static AdjustmentResult NotApplicable(VariantId variantId, string message)
        => new(true, variantId, message: message);

    public Result<AdjustmentResult> ToResult() => IsSuccess
        ? Result<AdjustmentResult>.Success(this)
        : Result<AdjustmentResult>.Failure(new Error("Adjustment.Failed", Error ?? string.Empty));
}