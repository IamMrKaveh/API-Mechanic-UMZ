namespace Domain.Inventory.Events;

public sealed class WarehouseActivatedEvent : DomainEvent
{
    public int WarehouseId { get; }
    public string WarehouseCode { get; }

    public WarehouseActivatedEvent(int warehouseId, string warehouseCode)
    {
        WarehouseId = warehouseId;
        WarehouseCode = warehouseCode;
    }
}