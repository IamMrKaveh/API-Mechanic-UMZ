using Domain.Common.Shared.ValueObjects;

namespace Domain.Inventory.ValueObjects;

public sealed class InventoryStatistics : ValueObject
{
    public int TotalVariants { get; }
    public int InStockVariants { get; }
    public int LowStockVariants { get; }
    public int OutOfStockVariants { get; }
    public int UnlimitedVariants { get; }
    public Money TotalInventoryValue { get; }
    public Money TotalSellingValue { get; }

    private InventoryStatistics(
        int totalVariants,
        int inStockVariants,
        int lowStockVariants,
        int outOfStockVariants,
        int unlimitedVariants,
        Money totalInventoryValue,
        Money totalSellingValue)
    {
        TotalVariants = totalVariants;
        InStockVariants = inStockVariants;
        LowStockVariants = lowStockVariants;
        OutOfStockVariants = outOfStockVariants;
        UnlimitedVariants = unlimitedVariants;
        TotalInventoryValue = totalInventoryValue;
        TotalSellingValue = totalSellingValue;
    }

    public static InventoryStatistics Create(
        int totalVariants,
        int inStockVariants,
        int lowStockVariants,
        int outOfStockVariants,
        int unlimitedVariants,
        decimal totalInventoryValue,
        decimal totalSellingValue)
    {
        Guard.Against.Negative(totalVariants, nameof(totalVariants));
        Guard.Against.Negative(inStockVariants, nameof(inStockVariants));
        Guard.Against.Negative(lowStockVariants, nameof(lowStockVariants));
        Guard.Against.Negative(outOfStockVariants, nameof(outOfStockVariants));
        Guard.Against.Negative(unlimitedVariants, nameof(unlimitedVariants));
        Guard.Against.Negative(Convert.ToInt32(totalInventoryValue), nameof(totalInventoryValue));
        Guard.Against.Negative(Convert.ToInt32(totalSellingValue), nameof(totalSellingValue));

        return new InventoryStatistics(
            totalVariants,
            inStockVariants,
            lowStockVariants,
            outOfStockVariants,
            unlimitedVariants,
            Money.FromDecimal(totalInventoryValue),
            Money.FromDecimal(totalSellingValue));
    }

    public static InventoryStatistics Empty() =>
        Create(0, 0, 0, 0, 0, 0, 0);

    // Computed Properties - Domain Logic
    public Money PotentialProfit => TotalSellingValue.Subtract(TotalInventoryValue);

    public decimal InStockPercentage =>
        TotalVariants > 0
            ? Math.Round((decimal)InStockVariants / TotalVariants * 100, 2)
            : 0;

    public decimal OutOfStockPercentage =>
        TotalVariants > 0
            ? Math.Round((decimal)OutOfStockVariants / TotalVariants * 100, 2)
            : 0;

    public decimal LowStockPercentage =>
        TotalVariants > 0
            ? Math.Round((decimal)LowStockVariants / TotalVariants * 100, 2)
            : 0;

    public decimal ProfitMargin =>
        TotalInventoryValue.Amount > 0
            ? Math.Round(PotentialProfit.Amount / TotalInventoryValue.Amount * 100, 2)
            : 0;

    public bool HasCriticalStock => OutOfStockVariants > 0 || LowStockVariants > TotalVariants * 0.2m;

    public bool IsHealthy => OutOfStockPercentage < 5 && LowStockPercentage < 10;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalVariants;
        yield return InStockVariants;
        yield return TotalInventoryValue;
        yield return TotalSellingValue;
    }
}