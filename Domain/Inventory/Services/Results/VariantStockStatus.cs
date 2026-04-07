using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Services.Results;

public sealed class VariantStockStatus(
    VariantId variantId,
    int availableStock,
    int requestedQuantity,
    bool isUnlimited,
    bool isAvailable)
{
    public VariantId VariantId { get; } = variantId;
    public int AvailableStock { get; } = availableStock;
    public int RequestedQuantity { get; } = requestedQuantity;
    public bool IsUnlimited { get; } = isUnlimited;
    public bool IsAvailable { get; } = isAvailable;
}