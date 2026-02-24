namespace Domain.Inventory;

/// <summary>
/// Aggregate انبار (Warehouse).
/// پشتیبانی از چند انبار (Multi-Warehouse) برای مدیریت موجودی توزیع‌شده.
/// </summary>
public sealed class Warehouse : AggregateRoot, ISoftDeletable, IActivatable, IAuditable
{
    private readonly List<WarehouseStock> _stocks = new();

    public WarehouseCode Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    public int Priority { get; private set; }

    
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    
    public IReadOnlyCollection<WarehouseStock> Stocks => _stocks.AsReadOnly();

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
        var stock = _stocks.FirstOrDefault(s => s.VariantId == variantId);
        if (stock is null)
        {
            stock = WarehouseStock.Create(Id, variantId);
            _stocks.Add(stock);
        }
        return stock;
    }

    public int GetAvailableStock(int variantId)
    {
        var stock = _stocks.FirstOrDefault(s => s.VariantId == variantId);
        return stock?.Available ?? 0;
    }

    public bool CanFulfill(int variantId, int quantity) =>
        GetAvailableStock(variantId) >= quantity;

    

    public void Update(string name, string city, string? address, string? phone, int priority)
    {
        EnsureNotDeleted();
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
        EnsureNotDeleted();
        if (IsActive) return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseActivatedEvent(Id, Code.Value));
    }

    public void Deactivate()
    {
        EnsureNotDeleted();
        if (!IsActive) return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseDeactivatedEvent(Id, Code.Value));
    }

    public void SetAsDefault()
    {
        EnsureNotDeleted();
        IsDefault = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new WarehouseSetAsDefaultEvent(Id, Code.Value));
    }

    public void Delete(int? deletedBy = null)
    {
        if (IsDeleted) return;
        if (IsDefault) throw new DomainException("امکان حذف انبار پیش‌فرض وجود ندارد.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted) throw new DomainException("این انبار حذف شده است.");
    }
}