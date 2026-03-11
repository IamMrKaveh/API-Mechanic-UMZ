namespace Domain.Inventory.Events;

public sealed class WarehouseDeactivatedEvent(int warehouseId, string warehouseCode) : DomainEvent
{
    public int WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = warehouseCode;
}