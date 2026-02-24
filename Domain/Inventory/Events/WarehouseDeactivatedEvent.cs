namespace Domain.Inventory.Events;

public sealed class WarehouseDeactivatedEvent : DomainEvent
{
    public int WarehouseId { get; }
    public string WarehouseCode { get; }

    public WarehouseDeactivatedEvent(int warehouseId, string warehouseCode)
    {
        WarehouseId = warehouseId;
        WarehouseCode = warehouseCode;
    }
}