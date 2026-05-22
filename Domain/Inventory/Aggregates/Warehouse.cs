using Domain.Inventory.ValueObjects;

namespace Domain.Inventory.Aggregates;

public sealed class Warehouse : AggregateRoot<WarehouseId>, IActivatable, IAuditable
{
    public WarehouseCode Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    public int Priority { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
}