namespace Domain.Inventory;

public sealed class Warehouse : Entity<Warehouse>, IActivatable, IAuditable
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

    public ICollection<WarehouseStock> Stocks { get; private set; } = [];

    private Warehouse()
    { }

    public static Warehouse Create(
        string code,
        string name,
        string city,
        string? address = null,
        string? phone = null,
        int priority = 0,
        bool isDefault = false)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(city, nameof(city));

        var codeVo = WarehouseCode.Create(code);

        var warehouse = new Warehouse
        {
            Code = codeVo,
            Name = name.Trim(),
            City = city.Trim(),
            Address = address?.Trim(),
            Phone = phone?.Trim(),
            Priority = priority,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        warehouse.AddDomainEvent(new WarehouseCreatedEvent(warehouse.Id, warehouse.Code.Value, warehouse.Name));
        return warehouse;
    }

    public WarehouseStock GetOrCreateStock(int variantId)
    {
        var stock = Stocks.FirstOrDefault(s => s.VariantId == variantId);
        if (stock is null)
        {
            stock = WarehouseStock.Create(Id, variantId);
            Stocks.Add(stock);
        }
        return stock;
    }

    public int GetAvailableStock(int variantId)
    {
        var stock = Stocks.FirstOrDefault(s => s.VariantId == variantId);
        return stock?.Available ?? 0;
    }

    public bool CanFulfill(int variantId, int quantity) =>
        GetAvailableStock(variantId) >= quantity;

    public void Update(string name, string city, string? address, string? phone, int priority)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));

        Name = name.Trim();
        City = city.Trim();
        Address = address?.Trim();
        Phone = phone?.Trim();
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseActivatedEvent(Id, Code.Value));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseDeactivatedEvent(Id, Code.Value));
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseSetAsDefaultEvent(Id, Code.Value));
    }

    public void RequestDeletion(int? deletedBy = null)
    {
        if (IsDefault)
            throw new DomainException("امکان حذف انبار پیش‌فرض وجود ندارد.");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseDeletedEvent(Id, Code.Value, deletedBy));
    }
}