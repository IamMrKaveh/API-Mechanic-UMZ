using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class WarehouseActivatedEvent(WarehouseId warehouseId, string warehouseCode) : DomainEvent
{
    public WarehouseId WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = warehouseCode;
}