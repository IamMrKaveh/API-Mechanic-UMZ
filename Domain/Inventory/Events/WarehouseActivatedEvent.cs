namespace Domain.Inventory.Events;

public sealed class WarehouseActivatedEvent(int warehouseId, string warehouseCode) : DomainEvent
{
    public int WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = warehouseCode;
}