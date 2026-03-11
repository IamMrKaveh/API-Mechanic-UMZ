namespace Domain.Inventory.Events;

public sealed class WarehouseDeletedEvent(int warehouseId, string code, int? deletedBy) : DomainEvent
{
    public int WarehouseId { get; } = warehouseId;
    public string Code { get; } = code;
    public int? DeletedBy { get; } = deletedBy;
}