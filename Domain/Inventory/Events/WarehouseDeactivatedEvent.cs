using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class WarehouseDeactivatedEvent(WarehouseId warehouseId, string warehouseCode) : DomainEvent
{
    public WarehouseId WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = warehouseCode;
}