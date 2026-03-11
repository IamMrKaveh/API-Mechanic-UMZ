namespace Domain.Inventory.ValueObjects;

public sealed class InventoryStatistics : ValueObject
{
    public int TotalQuantity { get; }
    public int ReservedQuantity { get; }
    public int SoldQuantity { get; }
    public int LowStockThreshold { get; }

    private InventoryStatistics(int totalQuantity, int reservedQuantity, int soldQuantity, int lowStockThreshold)
    {
        TotalQuantity = totalQuantity;
        ReservedQuantity = reservedQuantity;
        SoldQuantity = soldQuantity;
        LowStockThreshold = lowStockThreshold;
    }

    public static InventoryStatistics Create(int totalQuantity, int reservedQuantity, int soldQuantity, int lowStockThreshold)
    {
        if (totalQuantity < 0)
            throw new DomainException("Total quantity cannot be negative.");

        if (reservedQuantity < 0)
            throw new DomainException("Reserved quantity cannot be negative.");

        if (soldQuantity < 0)
            throw new DomainException("Sold quantity cannot be negative.");

        if (lowStockThreshold <= 0)
            throw new DomainException("Low stock threshold must be greater than zero.");

        return new InventoryStatistics(totalQuantity, reservedQuantity, soldQuantity, lowStockThreshold);
    }

    public int AvailableQuantity => TotalQuantity - ReservedQuantity;

    public bool IsLowStock => AvailableQuantity <= LowStockThreshold;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalQuantity;
        yield return ReservedQuantity;
        yield return SoldQuantity;
        yield return LowStockThreshold;
    }
}