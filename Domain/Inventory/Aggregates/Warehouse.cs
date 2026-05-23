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
        string code,
        string name,
        string city,
        string? address,
        string? phone,
        int priority,
        bool isDefault = false)
    {
        return new Warehouse
        {
            Id = WarehouseId.NewId(),
            Code = WarehouseCode.Create(code),
            Name = name,
            City = city,
            Address = address,
            Phone = phone,
            Priority = priority,
            IsActive = true,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string city, string? address, string? phone, int priority)
    {
        Name = name;
        City = city;
        Address = address;
        Phone = phone;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}