namespace Domain.Inventory.Events;

public sealed class WarehouseCreatedEvent : DomainEvent
{
    public int WarehouseId { get; }
    public string WarehouseCode { get; }
    public string WarehouseName { get; }

    public WarehouseCreatedEvent(int warehouseId, string code, string name)
    {
        WarehouseId = warehouseId;
        WarehouseCode = code;
        WarehouseName = name;
    }
}