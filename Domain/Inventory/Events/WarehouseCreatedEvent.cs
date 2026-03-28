using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class WarehouseCreatedEvent(WarehouseId warehouseId, string code, string name) : DomainEvent
{
    public WarehouseId WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = code;
    public string WarehouseName { get; } = name;
}