namespace Domain.Inventory.Events;

public sealed class WarehouseCreatedEvent(int warehouseId, string code, string name) : DomainEvent
{
    public int WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = code;
    public string WarehouseName { get; } = name;
}