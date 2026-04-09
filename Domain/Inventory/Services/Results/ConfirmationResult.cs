using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Services.Results;

public sealed class ConfirmationResult
{
    public bool IsSuccess { get; }
    public VariantId VariantId { get; }
    public int ConfirmedQuantity { get; }
    public string? Error { get; }

    private ConfirmationResult(bool isSuccess, VariantId variantId, int confirmedQuantity = 0, string? error = null)
    {
        IsSuccess = isSuccess;
        VariantId = variantId;
        ConfirmedQuantity = confirmedQuantity;
        Error = error;
    }

    public static ConfirmationResult Success(VariantId variantId, int quantity)
        => new(true, variantId, quantity);

    public static ConfirmationResult Failed(VariantId variantId, string error)
        => new(false, variantId, error: error);

    public Result<ConfirmationResult> ToResult() => IsSuccess
        ? Result<ConfirmationResult>.Success(this)
        : Result<ConfirmationResult>.Failure(new Error("Confirmation.Failed", Error ?? string.Empty));
}