using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Exceptions;

public class NegativeStockException(VariantId variantId, int currentStock, int requestedDeduction) : DomainException($"کسر {requestedDeduction} عدد از موجودی {currentStock} منجر به موجودی منفی ({currentStock - requestedDeduction}) می‌شود.")
{
    public VariantId VariantId { get; } = variantId;
    public int CurrentStock { get; } = currentStock;
    public int RequestedDeduction { get; } = requestedDeduction;
    public int ResultingStock { get; } = currentStock - requestedDeduction;

    public int GetShortage() => RequestedDeduction - CurrentStock;
}