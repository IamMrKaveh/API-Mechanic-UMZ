namespace Domain.Inventory.Services.Results;

public sealed class BatchStockStatus(IReadOnlyList<VariantStockStatus> items, bool allAvailable)
{
    public IReadOnlyList<VariantStockStatus> Items { get; } = items;
    public bool AllAvailable { get; } = allAvailable;
}