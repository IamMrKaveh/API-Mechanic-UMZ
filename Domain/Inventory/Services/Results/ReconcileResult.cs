using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Services.Results;

public sealed class ReconcileResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public bool HasDiscrepancy { get; }
    public VariantId VariantId { get; }
    public int FinalStock { get; }
    public int Difference { get; }
    public string? Message { get; }

    private ReconcileResult(
        bool isSuccess,
        VariantId variantId,
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

    public static ReconcileResult NotApplicable(VariantId variantId)
        => new(true, variantId, message: "واریانت نامحدود - انبارگردانی قابل اجرا نیست");

    public static ReconcileResult NoDiscrepancy(VariantId variantId, int stock)
        => new(true, variantId, finalStock: stock, message: "موجودی صحیح است - اختلافی وجود ندارد");

    public static ReconcileResult Corrected(VariantId variantId, int finalStock, int difference)
        => new(true, variantId, true, finalStock, difference, $"موجودی اصلاح شد. اختلاف: {difference:+#;-#;0}");
}