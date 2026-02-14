namespace Domain.Product;

public class AttributeType : AggregateRoot, IAuditable, ISoftDeletable, IActivatable
{
    public string Name { get; private set; } = null!;
    public string DisplayName { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Audit & Soft Delete
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    private readonly List<AttributeValue> _values = new();
    public IReadOnlyCollection<AttributeValue> Values => _values.AsReadOnly();

    /// <summary>
    /// Navigation alias برای EF Core — معادل Values
    /// </summary>
    public IReadOnlyCollection<AttributeValue> AttributeValues => _values.AsReadOnly();

    private AttributeType()
    { }

    public static AttributeType Create(string name, string displayName, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Attribute name is required.");

        return new AttributeType
        {
            Name = name.Trim(),
            DisplayName = displayName?.Trim() ?? name.Trim(),
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string displayName, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Attribute name is required.");
        if (string.IsNullOrWhiteSpace(displayName)) throw new DomainException("Attribute display name is required.");

        Name = name.Trim();
        DisplayName = displayName.Trim();
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public AttributeValue AddValue(string value, string displayValue, string? hexCode = null, int sortOrder = 0)
    {
        if (_values.Any(v => v.Value.Equals(value, StringComparison.OrdinalIgnoreCase) && !v.IsDeleted))
            throw new DomainException($"Attribute value '{value}' already exists.");

        var attributeValue = AttributeValue.Create(this, value, displayValue, hexCode, sortOrder);
        _values.Add(attributeValue);
        UpdatedAt = DateTime.UtcNow;
        return attributeValue;
    }

    public void UpdateValue(int valueId, string value, string displayValue, string? hexCode, int sortOrder, bool isActive)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId);
        if (attrValue == null) throw new DomainException("Attribute value not found.");

        attrValue.Update(value, displayValue, hexCode, sortOrder, isActive);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveValue(int valueId)
    {
        var attrValue = _values.FirstOrDefault(v => v.Id == valueId);
        if (attrValue == null) throw new DomainException("Attribute value not found.");

        attrValue.Delete();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(int? deletedBy)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;

        foreach (var val in _values)
        {
            val.Delete(deletedBy);
        }
    }
}