namespace Domain.Inventory.Exceptions;

public class NegativeStockException : DomainException
{
    public int VariantId { get; }
    public int CurrentStock { get; }
    public int RequestedDeduction { get; }
    public int ResultingStock { get; }

    public NegativeStockException(int variantId, int currentStock, int requestedDeduction)
        : base($"کسر {requestedDeduction} عدد از موجودی {currentStock} منجر به موجودی منفی ({currentStock - requestedDeduction}) می‌شود.")
    {
        VariantId = variantId;
        CurrentStock = currentStock;
        RequestedDeduction = requestedDeduction;
        ResultingStock = currentStock - requestedDeduction;
    }

    public int GetShortage() => RequestedDeduction - CurrentStock;
}