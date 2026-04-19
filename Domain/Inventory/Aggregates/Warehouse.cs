using Domain.Inventory.Events;
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

    private Warehouse()
    { }

    public static Warehouse Create(
        Guid rawId,
        string code,
        string name,
        string city,
        DateTime createdAt,
        string? address = null,
        string? phone = null,
        int priority = 0,
        bool isDefault = false)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(city, nameof(city));

        var warehouseId = WarehouseId.From(rawId);
        var codeVo = WarehouseCode.Create(code);

        var warehouse = new Warehouse
        {
            Id = warehouseId,
            Code = codeVo,
            Name = name.Trim(),
            City = city.Trim(),
            Address = address?.Trim(),
            Phone = phone?.Trim(),
            Priority = priority,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = createdAt
        };

        warehouse.RaiseDomainEvent(new WarehouseCreatedEvent(warehouseId, codeVo.Value, warehouse.Name));
        return warehouse;
    }

    public void Update(string name, string city, string? address, string? phone, int priority, DateTime updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        City = city.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        Priority = priority;
        UpdatedAt = updatedAt;
        IncrementVersion();
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new WarehouseActivatedEvent(Id, Code.Value));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new WarehouseDeactivatedEvent(Id, Code.Value));
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new WarehouseSetAsDefaultEvent(Id, Code.Value));
    }

    public void RequestDeletion(int? deletedBy = null)
    {
        if (IsDefault)
            throw new DomainException("امکان حذف انبار پیش‌فرض وجود ندارد.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new WarehouseDeletedEvent(Id, Code.Value, deletedBy));
    }
}