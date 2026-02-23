namespace Domain.Inventory.Events;

public sealed class WarehouseSetAsDefaultEvent : DomainEvent
{
    public int WarehouseId { get; }
    public string WarehouseCode { get; }

    public WarehouseSetAsDefaultEvent(int warehouseId, string warehouseCode)
    {
        WarehouseId = warehouseId;
        WarehouseCode = warehouseCode;
    }
}