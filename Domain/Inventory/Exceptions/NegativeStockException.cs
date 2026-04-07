using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Exceptions;

public sealed class NegativeStockException : DomainException
{
    public VariantId VariantId { get; }
    public int CurrentStock { get; }
    public int RequestedDeduction { get; }

    public override string ErrorCode => "NEGATIVE_STOCK";

    public NegativeStockException(VariantId variantId, int currentStock, int requestedDeduction)
        : base($"کسر {requestedDeduction} عدد از موجودی {currentStock} منجر به موجودی منفی ({currentStock - requestedDeduction}) می‌شود. برای واریانت با شناسه {variantId}")
    {
        VariantId = variantId;
        CurrentStock = currentStock;
        RequestedDeduction = requestedDeduction;
    }
}