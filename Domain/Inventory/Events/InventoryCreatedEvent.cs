using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Events;

public sealed record InventoryCreatedEvent(
        InventoryId InventoryId,
        VariantId VariantId,
        int InitialStock,
        bool IsUnlimited) : DomainEvent;
