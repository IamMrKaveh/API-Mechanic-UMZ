using Domain.Variant.ValueObjects;
using SharedKernel.Results;

namespace Domain.Inventory.Services.Results;

public sealed class ReleaseResult
{
    public bool IsSuccess { get; }
    public ProductVariantId VariantId { get; }
    public int ReleasedQuantity { get; }
    public string? Message { get; }

    private ReleaseResult(bool isSuccess, ProductVariantId variantId, int releasedQuantity = 0, string? message = null)
    {
        IsSuccess = isSuccess;
        VariantId = variantId;
        ReleasedQuantity = releasedQuantity;
        Message = message;
    }

    public static ReleaseResult Success(ProductVariantId variantId, int quantity)
        => new(true, variantId, quantity);

    public static ReleaseResult NotApplicable(ProductVariantId variantId, string message)
        => new(true, variantId, 0, message);

    public static ReleaseResult NothingToRelease(ProductVariantId variantId)
        => new(true, variantId, 0, "موجودی رزرو شده‌ای برای آزادسازی وجود ندارد");

    public Result<ReleaseResult> ToResult() => IsSuccess
        ? Result<ReleaseResult>.Success(this)
        : Result<ReleaseResult>.Failure(new Error("Release.Failed", Message ?? string.Empty));
}