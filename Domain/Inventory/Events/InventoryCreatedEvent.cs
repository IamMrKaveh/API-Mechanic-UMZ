using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class InventoryCreatedEvent(
        InventoryId InventoryId,
        VariantId VariantId,
        int InitialStock,
        bool IsUnlimited) : DomainEvent
{
    public InventoryId InventoryId { get; } = InventoryId;
    public VariantId VariantId { get; } = VariantId;
    public int InitialStock { get; } = InitialStock;
    public bool IsUnlimited { get; } = IsUnlimited;
}