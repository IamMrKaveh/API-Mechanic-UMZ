namespace Domain.Inventory.Services.Results;

public sealed class VariantStockStatus(
    ProductVariantId variantId,
    int availableStock,
    int requestedQuantity,
    bool isUnlimited,
    bool isAvailable)
{
    public ProductVariantId VariantId { get; } = variantId;
    public int AvailableStock { get; } = availableStock;
    public int RequestedQuantity { get; } = requestedQuantity;
    public bool IsUnlimited { get; } = isUnlimited;
    public bool IsAvailable { get; } = isAvailable;
}