using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Services.Results;

public sealed class ReconcileResult
{
    public bool IsSuccess { get; }
    public bool HasDiscrepancy { get; }
    public ProductVariantId VariantId { get; }
    public int FinalStock { get; }
    public int Difference { get; }
    public string? Message { get; }

    private ReconcileResult(
        bool isSuccess,
        ProductVariantId variantId,
        bool hasDiscrepancy = false,
        int finalStock = 0,
        int difference = 0,
        string? message = null)
    {
        IsSuccess = isSuccess;
        VariantId = variantId;
        HasDiscrepancy = hasDiscrepancy;
        FinalStock = finalStock;
        Difference = difference;
        Message = message;
    }

    public static ReconcileResult NotApplicable(ProductVariantId variantId)
        => new(true, variantId, message: "واریانت نامحدود - انبارگردانی قابل اجرا نیست");

    public static ReconcileResult NoDiscrepancy(ProductVariantId variantId, int stock)
        => new(true, variantId, finalStock: stock, message: "موجودی صحیح است - اختلافی وجود ندارد");

    public static ReconcileResult Corrected(ProductVariantId variantId, int finalStock, int difference)
        => new(true, variantId, true, finalStock, difference, $"موجودی اصلاح شد. اختلاف: {difference:+#;-#;0}");
}