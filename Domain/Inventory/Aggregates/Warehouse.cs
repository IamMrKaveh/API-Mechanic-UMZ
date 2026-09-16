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
        DateTime now,
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
            CreatedAt = now
        };
    }

    public void Update(string name, string city, string? address, string? phone, int priority, DateTime now)
    {
        Name = name;
        City = city;
        Address = address;
        Phone = phone;
        Priority = priority;
        UpdatedAt = now;
    }

    public void SetAsDefault(DateTime now)
    {
        IsDefault = true;
        UpdatedAt = now;
    }

    public void ClearDefault(DateTime now)
    {
        IsDefault = false;
        UpdatedAt = now;
    }

    public void Activate(DateTime now)
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTime now)
    {
        if (!IsActive) return;
        IsActive = false;
        UpdatedAt = now;
    }
}