using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Events;

public sealed class WarehouseDeletedEvent(WarehouseId warehouseId, string warehouseCode, int? deletedBy) : DomainEvent
{
    public WarehouseId WarehouseId { get; } = warehouseId;
    public string WarehouseCode { get; } = warehouseCode;
    public int? DeletedBy { get; } = deletedBy;
}