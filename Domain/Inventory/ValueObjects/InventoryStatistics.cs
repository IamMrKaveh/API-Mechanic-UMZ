namespace Domain.Inventory.ValueObjects;

public sealed class InventoryStatistics : ValueObject
{
    public StockQuantity TotalQuantity { get; }
    public StockQuantity ReservedQuantity { get; }
    public StockQuantity SoldQuantity { get; }
    public int LowStockThreshold { get; }

    private InventoryStatistics(
        StockQuantity totalQuantity,
        StockQuantity reservedQuantity,
        StockQuantity soldQuantity,
        int lowStockThreshold)
    {
        TotalQuantity = totalQuantity;
        ReservedQuantity = reservedQuantity;
        SoldQuantity = soldQuantity;
        LowStockThreshold = lowStockThreshold;
    }

    public static InventoryStatistics Create(
        int totalQuantity,
        int reservedQuantity,
        int soldQuantity,
        int lowStockThreshold)
    {
        if (lowStockThreshold <= 0)
            throw new DomainException("Low stock threshold must be greater than zero.");

        return new InventoryStatistics(
            StockQuantity.Create(totalQuantity),
            StockQuantity.Create(reservedQuantity),
            StockQuantity.Create(soldQuantity),
            lowStockThreshold);
    }

    public StockQuantity AvailableQuantity =>
        StockQuantity.Create(Math.Max(0, TotalQuantity.Value - ReservedQuantity.Value));

    public bool IsLowStock => AvailableQuantity.Value <= LowStockThreshold;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalQuantity;
        yield return ReservedQuantity;
        yield return SoldQuantity;
        yield return LowStockThreshold;
    }
}